using FileStorageDesign.Models;
using FileStorageDesign.Repositories;
using FileStorageDesign.Services;

namespace FileStorageDesign.Services
{
    public class CustomerDashboardService
    {
        private readonly CustomerService _customerService;
        private readonly OrderRepository _orderRepository;
        private readonly PaymentGateway _paymentGateway;
        private readonly EmailService _emailService;

        private readonly HashSet<int> _failPaymentsForCustomerIds;

        public CustomerDashboardService(
            CustomerService customerService,
            OrderRepository orderRepository,
            PaymentGateway paymentGateway,
            EmailService emailService,
            IEnumerable<int>? failPaymentsForCustomerIds = null)
        {
            _customerService = customerService;
            _orderRepository = orderRepository;
            _paymentGateway = paymentGateway;
            _emailService = emailService;

            _failPaymentsForCustomerIds =
                failPaymentsForCustomerIds?.ToHashSet()
                ?? new HashSet<int>();
        }

        public async Task<CustomerDashboard> LoadAsync(
    int customerId,
    CancellationToken ct)
        {
            // Start the four loads concurrently
            var customerTask =
                _customerService.GetCustomerAsync(customerId, ct);

            var ordersTask =
                _orderRepository.GetAllOrderAsync(ct);

            var paymentsTask =
                LoadPaymentsAsync(customerId, ct);

            var emailsTask =
                _emailService.GetAllEmailsAsync(ct);

            // Wait for all four loads with one await
            var allTasks = Task.WhenAll(
                customerTask,
                ordersTask,
                paymentsTask,
                emailsTask);

            try
            {
                await allTasks;
            }
            catch
            {
                var exceptions = allTasks.Exception?
                    .Flatten()
                    .InnerExceptions;

                if (exceptions != null)
                    throw new AggregateException(exceptions);

                throw;
            }

            // All tasks completed successfully
            var customer = customerTask.Result;

            var customerOrders = ordersTask.Result
                .Where(o => o.CustomerId == customerId)
                .ToList();

            var orderIds = customerOrders
                .Select(o => o.Id)
                .ToHashSet();

            var customerPayments = paymentsTask.Result
                .Where(p => orderIds.Contains(p.OrderId))
                .ToList();

            var customerEmails = emailsTask.Result
                .Where(e => e.CustomerId == customerId)
                .ToList();

            return new CustomerDashboard
            {
                Customer = customer,
                Orders = customerOrders,
                Payments = customerPayments,
                Emails = customerEmails
            };
        }
        

        private async Task<List<PaymentTransaction>> LoadPaymentsAsync(
            int customerId,
            CancellationToken ct)
        {
            var payments =
                await _paymentGateway.GetAllPaymentsAsync(ct);

            if (_failPaymentsForCustomerIds.Contains(customerId))
            {
                throw new TimeoutException(
                    "Payments store timeout");
            }

            return payments;
        }
    }
}