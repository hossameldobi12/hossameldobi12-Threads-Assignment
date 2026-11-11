
using System.Diagnostics;
using FileStorageDesign.Constants;
using FileStorageDesign.Exceptions;
using FileStorageDesign.Models;
using FileStorageDesign.Repositories;
using FileStorageDesign.logging;
using OrderProcessingSystem.Storage;

namespace FileStorageDesign.Services
{
    public class OrderProcessor
    {
        private readonly CustomerService _customerService;
        private readonly InventoryService _inventoryService;
        private readonly PricingService _pricingService;
        private readonly IPaymentGateway _paymentGateway;
        private readonly OrderRepository _orderRepository;
        private readonly IEmailService _emailService;
        private readonly JsonFileStore _jsonFileStore;

        public OrderProcessor(
            CustomerService customerService,
            InventoryService inventoryService,
            PricingService pricingService,
            IPaymentGateway paymentGateway,
            OrderRepository orderRepository,
            IEmailService emailService,
            JsonFileStore jsonFileStore)
        {
            _customerService = customerService;
            _inventoryService = inventoryService;
            _pricingService = pricingService;
            _paymentGateway = paymentGateway;
            _orderRepository = orderRepository;
            _emailService = emailService;
            _jsonFileStore = jsonFileStore;
        }

        public async Task<bool> ProcessOrderAsync(
            Order order,
            CancellationToken cancellationToken)
        {
            var totalStopwatch = Stopwatch.StartNew();

            Customer customer;

            // Step 1: Get Customer
            var stepWatch = Stopwatch.StartNew();

            try
            {
                Log.Info(nameof(OrderProcessor),
                    $"Getting customer for Order #{order.Id}");

                customer = await _customerService.GetCustomerAsync(
                    order.CustomerId,
                    cancellationToken);

                if (!customer.IsActive)
                {
                    order.Status = OrderStatus.Rejected;
                    order.FailureReason = "Customer is inactive";

                    await _orderRepository.SaveOrderAsync(
                        order, cancellationToken);

                    return false;
                }
            }
            catch (CustomerNotFoundException ex)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason = ex.Message;

                await _orderRepository.SaveOrderAsync(
                    order, cancellationToken);

                return false;
            }
            finally
            {
                stepWatch.Stop();

                Log.Info(nameof(OrderProcessor),
                    $"Customer step: {stepWatch.ElapsedMilliseconds} ms");
            }

            // Step 2: Reserve Stock
            stepWatch.Restart();

            try
            {
                bool reserved = await _inventoryService.ReserveStockAsync(
                    order, cancellationToken);

                if (!reserved)
                {
                    order.Status = OrderStatus.Rejected;

                    var products = await _jsonFileStore.ReadAsync<Product>(
                        DataFiles.Products, cancellationToken);

                    var failedItem = order.Items.FirstOrDefault(item =>
                        products.Any(product =>
                            product.Id == item.ProductId &&
                            product.StockQuantity < item.Quantity));

                    var productName = failedItem == null
                        ? "Unknown"
                        : products.First(p =>
                            p.Id == failedItem.ProductId).Name;

                    order.FailureReason =
                        $"Insufficient stock for '{productName}'";

                    await _orderRepository.SaveOrderAsync(
                        order, cancellationToken);

                    return false;
                }
            }
            finally
            {
                stepWatch.Stop();

                Log.Info(nameof(OrderProcessor),
                    $"Inventory step: {stepWatch.ElapsedMilliseconds} ms");
            }

            // Step 3: Calculate Total
            stepWatch.Restart();

            try
            {
                await _pricingService.CalculateTotalAsync(
                    order, cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                await _inventoryService.ReleaseStockAsync(
                    order, CancellationToken.None);

                throw;
            }
            catch (InvalidCouponException ex)
            {
                await _inventoryService.ReleaseStockAsync(
                    order, CancellationToken.None);

                order.Status = OrderStatus.Rejected;
                order.FailureReason = ex.Message;

                await _orderRepository.SaveOrderAsync(
                    order, CancellationToken.None);

                return false;
            }
            finally
            {
                stepWatch.Stop();

                Log.Info(nameof(OrderProcessor),
                    $"Pricing step: {stepWatch.ElapsedMilliseconds} ms");
            }

            // Step 4: Process Payment with 3-second timeout
            stepWatch.Restart();

            PaymentResult paymentResult;

            try
            {
                using var paymentCts =
                    CancellationTokenSource.CreateLinkedTokenSource(
                        cancellationToken);

                paymentCts.CancelAfter(TimeSpan.FromSeconds(3));

                paymentResult = await _paymentGateway.ProcessPaymentAsync(
                    order.Id,
                    order.TotalAmount,
                    paymentCts.Token);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                await _inventoryService.ReleaseStockAsync(
                    order, CancellationToken.None);

                throw;
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                await _inventoryService.ReleaseStockAsync(
                    order, CancellationToken.None);

                order.Status = OrderStatus.PaymentFailed;
                order.FailureReason = "Payment gateway timeout (3s)";

                await _orderRepository.SaveOrderAsync(
                    order, CancellationToken.None);

                return false;
            }
            finally
            {
                stepWatch.Stop();

                Log.Info(nameof(OrderProcessor),
                    $"Payment step: {stepWatch.ElapsedMilliseconds} ms");
            }

            // Payment Declined
            if (!paymentResult.Success)
            {
                await _inventoryService.ReleaseStockAsync(
                    order, CancellationToken.None);

                order.Status = OrderStatus.PaymentFailed;
                order.FailureReason = paymentResult.Message;

                await _orderRepository.SaveOrderAsync(
                    order, CancellationToken.None);

                return false;
            }

            order.TransactionId = paymentResult.TransactionId;

            // Step 5: Save Completed Order
            stepWatch.Restart();

            try
            {
                order.Status = OrderStatus.Completed;

                await _orderRepository.SaveOrderAsync(
                    order, cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                await _paymentGateway.RefundAsync(
                    order.TransactionId!, CancellationToken.None);

                await _inventoryService.ReleaseStockAsync(
                    order, CancellationToken.None);

                throw;
            }
            catch (DatabaseWriteException)
            {
                await _paymentGateway.RefundAsync(
                    order.TransactionId!, CancellationToken.None);

                await _inventoryService.ReleaseStockAsync(
                    order, CancellationToken.None);

                order.Status = OrderStatus.Failed;

                order.FailureReason =
                    $"Database save failed after payment; payment {order.TransactionId} refunded";

                await _orderRepository.SaveOrderAsync(
                    order, CancellationToken.None);

                return false;
            }
            finally
            {
                stepWatch.Stop();

                Log.Info(nameof(OrderProcessor),
                    $"Order save step: {stepWatch.ElapsedMilliseconds} ms");
            }

            // Step 6: Send Confirmation Email
            stepWatch.Restart();

            try
            {
                await SendConfirmationWithRetryAsync(
                    order, customer, cancellationToken);
            }
            finally
            {
                stepWatch.Stop();

                Log.Info(nameof(OrderProcessor),
                    $"Email step: {stepWatch.ElapsedMilliseconds} ms");
            }

            totalStopwatch.Stop();

            Log.Success(nameof(OrderProcessor),
                $"Order #{order.Id} processed in {totalStopwatch.ElapsedMilliseconds} ms");

            return order.Status == OrderStatus.Completed;
        }

        private async Task SendConfirmationWithRetryAsync(
            Order order,
            Customer customer,
            CancellationToken cancellationToken)
        {
            int[] retryDelays = { 500, 1000, 2000 };

            for (int attempt = 1; attempt <= 4; attempt++)
            {
                try
                {
                    await _emailService.SendConfirmationAsync(
                        order, customer, cancellationToken);

                    return;
                }
                catch (SmtpUnavailableException ex)
                {
                    Log.Error(nameof(OrderProcessor),
                        $"Email attempt {attempt} failed for Order #{order.Id}: {ex.Message}");

                    if (attempt == 4)
                    {
                        await SaveEmailForRetryAsync(
                            order,
                            customer,
                            attempt,
                            ex.Message,
                            cancellationToken);

                        Log.Info(nameof(OrderProcessor),
                            $"Email for Order #{order.Id} queued for retry later");

                        return;
                    }

                    await Task.Delay(
                        retryDelays[attempt - 1],
                        cancellationToken);
                }
            }
        }

        private async Task SaveEmailForRetryAsync(
            Order order,
            Customer customer,
            int attempts,
            string lastError,
            CancellationToken cancellationToken)
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

            await _jsonFileStore.UpdateAsync<EmailRetryRecord>(
                DataFiles.EmailRetry,
                records => records.Add(retryRecord),
                cancellationToken);
        }
    }
}
