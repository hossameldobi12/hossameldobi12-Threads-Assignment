
using FileStorageDesign.Models;
using FileStorageDesign.Repositories;
using FileStorageDesign.Services;
using FileStorageDesign.Storage;
using Mini_Project___Order_Processing_System.Runners;
using System.Diagnostics;

namespace FileStorageDesign.Runners
{
    public class Phase2Runner
    {
        private readonly IncomingOrderRepository _incomingOrderRepository;
        private readonly OrderPricerService _orderPricerService;
        private readonly DataSeeder _dataSeeder;
        private readonly RaceConditionDemo _raceConditionDemo;
        private readonly OrderValidatorService _orderValidatorService;

        private readonly OrderProcessorV2 _orderProcessor;
        private readonly ProductRepository _productRepository;
        private readonly OrderRepository _orderRepository;
        private readonly ReportGenerator _reportGenerator;

        public Phase2Runner(
            IncomingOrderRepository incomingOrderRepository,
            OrderPricerService orderPricerService,
            DataSeeder dataSeeder,
            RaceConditionDemo raceConditionDemo,
            OrderValidatorService orderValidatorService,
            OrderProcessorV2 orderProcessor,
            ProductRepository productRepository,
            OrderRepository orderRepository,
            ReportGenerator reportGenerator)
        {
            _incomingOrderRepository = incomingOrderRepository;
            _orderPricerService = orderPricerService;
            _dataSeeder = dataSeeder;
            _raceConditionDemo = raceConditionDemo;
            _orderValidatorService = orderValidatorService;
            _orderProcessor = orderProcessor;
            _productRepository = productRepository;
            _orderRepository = orderRepository;
            _reportGenerator = reportGenerator;
        }

        public async Task RunAsync(CancellationToken ct)
        {
            Console.WriteLine("\n========== Phase 2 ==========");

            // Reset data
            await _dataSeeder.ResetAsync(ct);

            // Race condition demonstration
            await _raceConditionDemo.RunAsync(ct);

            // Reset data again
            await _dataSeeder.ResetAsync(ct);

            // Pricing comparison
            await RunPricingComparisonAsync(ct);

            // Concurrent validation
            await RunConcurrentValidationAsync(ct);
            // Process validated orders
            await RunProcessingOrdersAsync(ct);
            await RunReportGenerationAsync(ct);
        }
        private async Task RunReportGenerationAsync(CancellationToken ct)
        {
            Console.WriteLine("\n========== Report Generation ==========");

            var reportTask = _reportGenerator.GenerateAsync(ct);

            Console.WriteLine("Generating report...");

            var report = await reportTask;

            Console.WriteLine("\n========== Final Report ==========");
            Console.WriteLine($"Total Orders: {report.TotalOrders}");
            Console.WriteLine($"Completed: {report.CompletedCount}");
            Console.WriteLine($"Rejected: {report.RejectedCount}");
            Console.WriteLine($"Failed: {report.FailedCount}");
            Console.WriteLine($"Payment Failed: {report.PaymentFailedCount}");
            Console.WriteLine($"Total Revenue: {report.TotalRevenue}");
            Console.WriteLine($"Average Order Value: {report.AverageOrderValue}");
            Console.WriteLine($"Top Selling Product: {report.TopSellingProduct}");
            Console.WriteLine($"Top Selling Quantity: {report.TopSellingQuantity}");
            Console.WriteLine($"Generated At: {report.GeneratedAt}");
        }
        private async Task RunProcessingOrdersAsync(CancellationToken ct)
        {
            Console.WriteLine("\n========== Processing Validated Orders ==========");

            var orders = await _orderRepository.GetByStatusAsync(
                OrderStatus.Validated, ct);

            var processingTasks = new List<Task<bool>>();
            var processingOrders = new List<Order>();

            var stopwatch = Stopwatch.StartNew();

            // Start processing all validated orders
            foreach (var order in orders)
            {
                processingOrders.Add(order);

                processingTasks.Add(
                    _orderProcessor.ProcessValidatedOrderAsync(order, ct));
            }

            if (processingTasks.Count > 0)
            {
                Console.WriteLine(
                    $"First task status immediately: {processingTasks[0].Status}");
            }

            // Give tasks time to start
            await Task.Delay(1000, ct);

            Console.WriteLine("\nTask statuses after 1 second:");

            for (int i = 0; i < processingTasks.Count; i++)
            {
                Console.WriteLine(
                    $"Order #{processingOrders[i].Id}: {processingTasks[i].Status}");
            }

            // Await every task without stopping when one fails
            for (int i = 0; i < processingTasks.Count; i++)
            {
                var order = processingOrders[i];
                var task = processingTasks[i];

                try
                {
                    bool result = await task;

                    Console.WriteLine(
                        $"Order #{order.Id}: {task.Status}, Result: {result}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Order #{order.Id} failed: {ex.Message}");

                    order.Status = OrderStatus.Failed;
                    order.FailureReason = ex.Message;

                    // Release the stock reserved by this order
                    foreach (var item in order.Items)
                    {
                        await _productRepository.ReleaseStockAsync(
                            item.ProductId,
                            item.Quantity,
                            ct);
                    }

                    await _orderRepository.SaveOrderAsync(order, ct);
                    var allOrders = await _orderRepository.GetAllOrderAsync(ct);

                    Console.WriteLine("\n========== Final Orders Summary ==========");

                    Console.WriteLine($"Total Orders: {allOrders.Count}");

                    Console.WriteLine($"Completed: {allOrders.Count(o => o.Status == OrderStatus.Completed)}");

                    Console.WriteLine($"Rejected: {allOrders.Count(o => o.Status == OrderStatus.Rejected)}");

                    Console.WriteLine($"Failed: {allOrders.Count(o => o.Status == OrderStatus.Failed)}");

                    Console.WriteLine($"Payment Failed: {allOrders.Count(o => o.Status == OrderStatus.PaymentFailed)}");
                }
            }

            stopwatch.Stop();

            Console.WriteLine("\nFinal task statuses:");

            for (int i = 0; i < processingTasks.Count; i++)
            {
                Console.WriteLine(
                    $"Order #{processingOrders[i].Id}: {processingTasks[i].Status}");
            }

            Console.WriteLine(
                $"Total processing time: {stopwatch.ElapsedMilliseconds} ms");
        }

        private async Task RunPricingComparisonAsync(CancellationToken ct)
        {
            Console.WriteLine("\n========== Pricing Comparison ==========");

            var orders = await _incomingOrderRepository.GetAllAsync(ct);

            var originalOrder = orders.FirstOrDefault(o => o.Id == 1);

            if (originalOrder == null)
            {
                Console.WriteLine("Order #1 not found.");
                return;
            }

            // Sequential pricing
            var sequentialOrder = CloneOrder(originalOrder);

            var stopwatch = Stopwatch.StartNew();

            await _orderPricerService.PriceOrderSequentialAsync(
                sequentialOrder, ct);

            stopwatch.Stop();

            var sequentialTime = stopwatch.ElapsedMilliseconds;

            // Concurrent pricing
            var concurrentOrder = CloneOrder(originalOrder);

            stopwatch.Restart();

            await _orderPricerService.PriceOrderAsync(
                concurrentOrder, ct);

            stopwatch.Stop();

            var concurrentTime = stopwatch.ElapsedMilliseconds;

            // Print results
            Console.WriteLine("\nSequential Pricing:");
            PrintOrderPricing(sequentialOrder);
            Console.WriteLine($"Execution Time: {sequentialTime} ms");

            Console.WriteLine("\nConcurrent Pricing:");
            PrintOrderPricing(concurrentOrder);
            Console.WriteLine($"Execution Time: {concurrentTime} ms");

            Console.WriteLine("\n========== Time Comparison ==========");

            Console.WriteLine($"Sequential: {sequentialTime} ms");
            Console.WriteLine($"Concurrent: {concurrentTime} ms");

            Console.WriteLine("=====================================");
        }

        private async Task RunConcurrentValidationAsync(CancellationToken ct)
        {
            Console.WriteLine("\n========== Concurrent Validation ==========");

            var orders = await _incomingOrderRepository.GetAllAsync(ct);

            var validationTasks = new List<Task>();

            foreach (var order in orders)
            {
                validationTasks.Add(
                    _orderValidatorService.ValidateOrderAsync(order, ct));
            }

            await Task.WhenAll(validationTasks);

            Console.WriteLine($"Total orders validated: {validationTasks.Count}");
            Console.WriteLine("Concurrent validation completed.");
        }

        private static Order CloneOrder(Order order)
        {
            return new Order
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                Items = order.Items.Select(item => new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                }).ToList(),
                CouponCode = order.CouponCode,
                SubTotal = order.SubTotal,
                Discount = order.Discount,
                ShippingCost = order.ShippingCost,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                CreatedAt = order.CreatedAt,
                FailureReason = order.FailureReason,
                TransactionId = order.TransactionId
            };
        }

        private static void PrintOrderPricing(Order order)
        {
            Console.WriteLine($"Order ID: {order.Id}");
            Console.WriteLine($"SubTotal: {order.SubTotal}");
            Console.WriteLine($"Discount: {order.Discount}");
            Console.WriteLine($"Shipping Cost: {order.ShippingCost}");
            Console.WriteLine($"Total Amount: {order.TotalAmount}");
        }
    }
}
