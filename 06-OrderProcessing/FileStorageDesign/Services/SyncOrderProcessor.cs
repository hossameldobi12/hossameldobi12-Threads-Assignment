
using FileStorageDesign.Constants;
using FileStorageDesign.Exceptions;
using FileStorageDesign.logging;
using FileStorageDesign.Models;
using FileStorageDesign.Repositories;
using OrderProcessingSystem.Storage;
using System.Diagnostics;

namespace FileStorageDesign.Services
{
    public class SyncOrderProcessor
    {
        private readonly CustomerRepository _customerRepository;
        private readonly SyncInventoryService _inventoryService;
        private readonly SyncPricingService _pricingService;
        private readonly SyncPaymentGateway _paymentGateway;
        private readonly OrderRepository _orderRepository;
        private readonly SyncEmailService _emailService;
        private readonly JsonFileStore _jsonFileStore;

        public SyncOrderProcessor(
            CustomerRepository customerRepository,
            SyncInventoryService inventoryService,
            SyncPricingService pricingService,
            SyncPaymentGateway paymentGateway,
            OrderRepository orderRepository,
            SyncEmailService emailService,
            JsonFileStore jsonFileStore)
        {
            _customerRepository = customerRepository;
            _inventoryService = inventoryService;
            _pricingService = pricingService;
            _paymentGateway = paymentGateway;
            _orderRepository = orderRepository;
            _emailService = emailService;
            _jsonFileStore = jsonFileStore;
        }

        public bool ProcessOrder(Order order)
        {
            var totalStopwatch = Stopwatch.StartNew();

            Log.Info(nameof(SyncOrderProcessor),
                $"Starting Order #{order.Id}");

            // Step 1: Validate Customer
            var stepStopwatch = Stopwatch.StartNew();

            Customer? customer;

            try
            {
                customer = _customerRepository.GetCustomerById(
                    order.CustomerId);

                if (customer == null)
                    throw new CustomerNotFoundException(
                        $"Customer #{order.CustomerId} not found");

                if (!customer.IsActive)
                {
                    order.Status = OrderStatus.Rejected;
                    order.FailureReason = "Customer is inactive";

                    _orderRepository.SaveOrder(order);

                    return false;
                }
            }
            catch (CustomerNotFoundException ex)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason = ex.Message;

                _orderRepository.SaveOrder(order);

                return false;
            }

            stepStopwatch.Stop();

            Log.Info(nameof(SyncOrderProcessor),
                $"Customer validation completed in {stepStopwatch.ElapsedMilliseconds}ms");

            // Step 2: Reserve Inventory
            stepStopwatch.Restart();

            bool reserved = _inventoryService.ReserveStock(order);

            if (!reserved)
            {
                order.Status = OrderStatus.Rejected;

                var products = _jsonFileStore.Read<Product>(
                    DataFiles.Products);

                var failedProduct = order.Items.FirstOrDefault(item =>
                {
                    var product = products.FirstOrDefault(
                        p => p.Id == item.ProductId);

                    return product == null
                        || product.StockQuantity < item.Quantity;
                });

                if (failedProduct != null)
                {
                    var product = products.FirstOrDefault(
                        p => p.Id == failedProduct.ProductId);

                    string productName = product?.Name
                        ?? $"Product ID {failedProduct.ProductId}";

                    order.FailureReason =
                        $"Insufficient stock for '{productName}'";
                }
                else
                {
                    order.FailureReason = "Unable to reserve stock";
                }

                _orderRepository.SaveOrder(order);

                return false;
            }

            stepStopwatch.Stop();

            Log.Info(nameof(SyncOrderProcessor),
                $"Inventory reservation completed in {stepStopwatch.ElapsedMilliseconds}ms");

            // Step 3: Calculate Pricing
            stepStopwatch.Restart();

            try
            {
                _pricingService.CalculateTotal(order);
            }
            catch (InvalidCouponException ex)
            {
                _inventoryService.ReleaseStock(order);

                order.Status = OrderStatus.Rejected;
                order.FailureReason = ex.Message;

                _orderRepository.SaveOrder(order);

                return false;
            }

            stepStopwatch.Stop();

            Log.Info(nameof(SyncOrderProcessor),
                $"Pricing completed in {stepStopwatch.ElapsedMilliseconds}ms");

            // Step 4: Process Payment
            stepStopwatch.Restart();

            PaymentResult paymentResult =
                _paymentGateway.ProcessPayment(
                    order.Id,
                    order.TotalAmount);

            if (!paymentResult.Success)
            {
                _inventoryService.ReleaseStock(order);

                order.Status = OrderStatus.PaymentFailed;
                order.FailureReason = paymentResult.Message;

                _orderRepository.SaveOrder(order);

                return false;
            }

            order.TransactionId = paymentResult.TransactionId;

            stepStopwatch.Stop();

            Log.Info(nameof(SyncOrderProcessor),
                $"Payment completed in {stepStopwatch.ElapsedMilliseconds}ms");

            // Step 5: Save Completed Order
            stepStopwatch.Restart();

            order.Status = OrderStatus.Completed;

            try
            {
                _orderRepository.SaveOrder(order);
            }
            catch (DatabaseWriteException ex)
            {
                _paymentGateway.Refund(order.TransactionId!);
                _inventoryService.ReleaseStock(order);

                order.Status = OrderStatus.Failed;
                order.FailureReason =
                    $"Database save failed after payment; payment {order.TransactionId} refunded. {ex.Message}";

                _orderRepository.SaveOrder(order);

                return false;
            }

            stepStopwatch.Stop();

            Log.Info(nameof(SyncOrderProcessor),
                $"Order saved in {stepStopwatch.ElapsedMilliseconds}ms");

            // Step 6: Send Confirmation Email with Retry
            stepStopwatch.Restart();

            SendConfirmationWithRetry(order, customer);

            stepStopwatch.Stop();

            Log.Info(nameof(SyncOrderProcessor),
                $"Email processing completed in {stepStopwatch.ElapsedMilliseconds}ms");

            totalStopwatch.Stop();

            Log.Success(nameof(SyncOrderProcessor),
                $"Order #{order.Id} completed in {totalStopwatch.ElapsedMilliseconds}ms");

            return true;
        }

        private void SendConfirmationWithRetry(
            Order order,
            Customer customer)
        {
            int[] retryDelays = { 500, 1000, 2000 };

            for (int attempt = 1; attempt <= 4; attempt++)
            {
                try
                {
                    _emailService.SendConfirmation(order, customer);
                    return;
                }
                catch (SmtpUnavailableException ex)
                {
                    Log.Error(nameof(SyncOrderProcessor),
                        $"Email attempt {attempt} failed for Order #{order.Id}: {ex.Message}");

                    if (attempt == 4)
                    {
                        SaveEmailForRetry(order, customer, attempt, ex.Message);

                        Log.Info(nameof(SyncOrderProcessor),
                            $"Email for Order #{order.Id} added to retry queue");

                        return;
                    }

                    Thread.Sleep(retryDelays[attempt - 1]);
                }
            }
        }

        private void SaveEmailForRetry(
            Order order,
            Customer customer,
            int attempts,
            string lastError)
        {
            var retryRecord = new EmailRetryRecord
            {
                OrderId = order.Id,
                CustomerId = customer.Id,
                EmailAddress = customer.Email,
                Attempts = attempts,
                LastError = lastError,
                FailedAt = DateTime.UtcNow
            };

            _jsonFileStore.Update<EmailRetryRecord>(
                DataFiles.EmailRetry,
                records => records.Add(retryRecord));
        }
    }
}
