
using FileStorageDesign.Models;
using FileStorageDesign.Repositories;

namespace FileStorageDesign.Services
{
    public class FireAndForgetInvestigation
    {
        private readonly OrderRepository _orderRepository;
        private readonly IEmailService _emailService;

        public FireAndForgetInvestigation(
            OrderRepository orderRepository,
            IEmailService emailService)
        {
            _orderRepository = orderRepository;
            _emailService = emailService;
        }

        public async Task ProcessOrderAsync(Order order, Customer customer, CancellationToken cancellationToken = default)
        {
            await _orderRepository.SaveOrderAsync(order, cancellationToken);

#pragma warning disable CS4014
            _emailService.SendConfirmationAsync(order, customer, cancellationToken);
#pragma warning restore CS4014

            Console.WriteLine("Order processing finished");
        }
    }
}
