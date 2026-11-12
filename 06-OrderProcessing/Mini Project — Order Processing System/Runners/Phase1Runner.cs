using FileStorageDesign.Models;
using FileStorageDesign.Repositories;
using FileStorageDesign.Services;
using FileStorageDesign.Storage;
using System.Diagnostics;

namespace Mini_Project___Order_Processing_System.Runners
{
    public class Phase1Runner
    {
        private readonly DataSeeder _dataSeeder;
        private readonly IncomingOrderRepository _incomingOrderRepository;
        private readonly SyncOrderValidator _syncOrderValidator;
        private readonly OrderValidatorService _asyncOrderValidator;
        private readonly OrderRepository _orderRepository;

        public Phase1Runner(
            DataSeeder dataSeeder,
            IncomingOrderRepository incomingOrderRepository,
            SyncOrderValidator syncOrderValidator,
            OrderValidatorService asyncOrderValidator,
            OrderRepository orderRepository)
        {
            _dataSeeder = dataSeeder;
            _incomingOrderRepository = incomingOrderRepository;
            _syncOrderValidator = syncOrderValidator;
            _asyncOrderValidator = asyncOrderValidator;
            _orderRepository = orderRepository;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            // Reset data before Sync Run
            await _dataSeeder.ResetAsync(cancellationToken);

            // Load incoming orders
            var syncOrders = await _incomingOrderRepository
                .GetAllAsync(cancellationToken);

            Console.WriteLine("\n========== SYNC RUN ==========");

            var syncStopwatch = Stopwatch.StartNew();

            foreach (var order in syncOrders)
            {
                cancellationToken.ThrowIfCancellationRequested();

                _syncOrderValidator.ValidateOrder(order);
            }

            syncStopwatch.Stop();

            Console.WriteLine($"Sync Orders Processed: {syncOrders.Count}");
            Console.WriteLine($"Sync Elapsed Time: {syncStopwatch.ElapsedMilliseconds} ms");


            // Reset data before Async Run
            await _dataSeeder.ResetAsync(cancellationToken);

            // Load incoming orders again
            var asyncOrders = await _incomingOrderRepository
                .GetAllAsync(cancellationToken);

            Console.WriteLine("\n========== ASYNC RUN ==========");

            var asyncStopwatch = Stopwatch.StartNew();

            foreach (var order in asyncOrders)
            {
                await _asyncOrderValidator.ValidateOrderAsync(
                    order,
                    cancellationToken);
            }

            asyncStopwatch.Stop();

            Console.WriteLine($"Async Orders Processed: {asyncOrders.Count}");
            Console.WriteLine($"Async Elapsed Time: {asyncStopwatch.ElapsedMilliseconds} ms");


            // Read persisted results from orders.json
            var validatedOrders = await _orderRepository
                .GetByStatusAsync(OrderStatus.Validated, cancellationToken);

            var rejectedOrders = await _orderRepository
                .GetByStatusAsync(OrderStatus.Rejected, cancellationToken);

            Console.WriteLine("\n========== FINAL SUMMARY ==========");

            Console.WriteLine($"Total Orders: {validatedOrders.Count + rejectedOrders.Count}");
            Console.WriteLine($"Validated Orders: {validatedOrders.Count}");
            Console.WriteLine($"Rejected Orders: {rejectedOrders.Count}");

            Console.WriteLine("\n========== REJECTED ORDERS DETAILS ==========");

            foreach (var order in rejectedOrders)
            {
                Console.WriteLine(
                    $"Order #{order.Id} -> {order.FailureReason}");
            }
        }
    }
}