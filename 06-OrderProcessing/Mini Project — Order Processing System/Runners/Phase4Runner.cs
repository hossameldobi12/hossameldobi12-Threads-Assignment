
using System.Collections.Concurrent;
using System.Diagnostics;
using FileStorageDesign.Constants;
using FileStorageDesign.Exceptions;
using FileStorageDesign.Models;
using FileStorageDesign.Repositories;
using FileStorageDesign.Services;
using FileStorageDesign.Storage;
using OrderProcessingSystem.Storage;

namespace FileStorageDesign.Runners
{
    public class Phase4Runner
    {
        private readonly DataSeeder _dataSeeder;
        private readonly IncomingOrderRepository _incomingOrderRepository;
        private readonly OrderProcessor _orderProcessor;
        private readonly JsonFileStore _jsonFileStore;
        private readonly CustomerDashboardService _customerDashboardService;
        private readonly ShippingQuoteService _shippingQuoteService;
        private readonly IEmailService _emailService;

        public Phase4Runner(
            DataSeeder dataSeeder,
            IncomingOrderRepository incomingOrderRepository,
            OrderProcessor orderProcessor,
            JsonFileStore jsonFileStore,
            CustomerDashboardService customerDashboardService,
            ShippingQuoteService shippingQuoteService,
            IEmailService emailService)
        {
            _dataSeeder = dataSeeder;
            _incomingOrderRepository = incomingOrderRepository;
            _orderProcessor = orderProcessor;
            _jsonFileStore = jsonFileStore;
            _customerDashboardService = customerDashboardService;
            _shippingQuoteService = shippingQuoteService;
            _emailService = emailService;
        }

        // Task 10.1 - Live Checkout Progress
        public async Task RunTask10_1Async(CancellationToken ct)
        {
            Console.WriteLine(
                "\n========== TASK 10.1 - LIVE CHECKOUT PROGRESS ==========\n");

            await _dataSeeder.ResetAsync(ct);

            var orders =
                await _incomingOrderRepository.GetAllAsync(ct);

            Console.WriteLine($"Starting {orders.Count} orders...\n");

            var tasks = orders
                .Select(order => ProcessOrderWithTimingAsync(order, ct))
                .ToList();

            int completedCount = 0;

            await foreach (var task in Task.WhenEach(tasks))
            {
                var result = await task;

                completedCount++;

                string failureReason =
                    string.IsNullOrWhiteSpace(result.Order.FailureReason)
                        ? ""
                        : $" ({result.Order.FailureReason})";

                Console.WriteLine(
                    $"[{completedCount,2}/20] " +
                    $"Order #{result.Order.Id} -> " +
                    $"{result.Order.Status}{failureReason} " +
                    $"after {result.ElapsedSeconds:F1} s");
            }

            var savedOrders =
                await _jsonFileStore.ReadAsync<Order>(
                    DataFiles.Orders, ct);

            PrintSummary(savedOrders);
        }

        private async Task<OrderResult> ProcessOrderWithTimingAsync(
            Order order,
            CancellationToken ct)
        {
            var stopwatch = Stopwatch.StartNew();

            await _orderProcessor.ProcessOrderAsync(order, ct);

            stopwatch.Stop();

            return new OrderResult(
                order,
                stopwatch.Elapsed.TotalSeconds);
        }

        private static void PrintSummary(List<Order> orders)
        {
            Console.WriteLine("\n========== TASK 10.1 SUMMARY ==========");

            Console.WriteLine($"Total Orders: {orders.Count}");

            Console.WriteLine(
                $"Completed: {orders.Count(o => o.Status == OrderStatus.Completed)}");

            Console.WriteLine(
                $"Rejected: {orders.Count(o => o.Status == OrderStatus.Rejected)}");

            Console.WriteLine(
                $"PaymentFailed: {orders.Count(o => o.Status == OrderStatus.PaymentFailed)}");

            Console.WriteLine(
                $"Failed: {orders.Count(o => o.Status == OrderStatus.Failed)}");
        }

        // Task 10.2 - Customer Dashboard
        public async Task RunTask10_2Async(CancellationToken ct)
        {
            Console.WriteLine(
                "\n========== TASK 10.2 - CUSTOMER DASHBOARD ==========\n");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var dashboard =
                    await _customerDashboardService.LoadAsync(3, ct);

                stopwatch.Stop();

                Console.WriteLine(
                    $"Customer: {dashboard.Customer.Name}");

                Console.WriteLine(
                    $"Orders: {dashboard.Orders.Count}");

                foreach (var order in dashboard.Orders)
                {
                    Console.WriteLine(
                        $"  Order #{order.Id} -> {order.Status}");
                }

                Console.WriteLine(
                    $"Payments: {dashboard.Payments.Count}");

                foreach (var payment in dashboard.Payments)
                {
                    Console.WriteLine(
                        $"  Order #{payment.OrderId} -> {payment.Status}");
                }

                Console.WriteLine(
                    $"Emails: {dashboard.Emails.Count}");

                Console.WriteLine(
                    $"Customer #3 dashboard loaded in " +
                    $"{stopwatch.Elapsed.TotalSeconds:F2} seconds");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                Console.WriteLine(
                    $"Customer #3 failed: {ex.Message}");
            }

            Console.WriteLine("\nTesting Customer #99...\n");

            stopwatch.Restart();

            try
            {
                await _customerDashboardService.LoadAsync(99, ct);
            }
            catch (AggregateException ex)
            {
                stopwatch.Stop();

                foreach (var exception in ex.Flatten().InnerExceptions)
                {
                    Console.WriteLine(
                        $"Error: {exception.Message}");
                }

                Console.WriteLine(
                    $"Customer #99 load finished in " +
                    $"{stopwatch.Elapsed.TotalSeconds:F2} seconds");
            }
        }

        // Task 10.3 - Fastest Shipping Quote
        public async Task RunTask10_3Async(CancellationToken ct)
        {
            Console.WriteLine(
                "\n========== TASK 10.3 - FASTEST SHIPPING QUOTE ==========\n");

            var orders =
                await _jsonFileStore.ReadAsync<Order>(
                    DataFiles.Orders, ct);

            for (int orderId = 1; orderId <= 5; orderId++)
            {
                var order = orders.FirstOrDefault(
                    o => o.Id == orderId);

                if (order == null)
                {
                    Console.WriteLine(
                        $"Order #{orderId} not found.");

                    continue;
                }

                Console.WriteLine($"\nTesting Order #{order.Id}...\n");

                var quote =
                    await _shippingQuoteService.GetFastestQuoteAsync(
                        order, ct);

                Console.WriteLine($"Order: #{order.Id}");
                Console.WriteLine($"Winning Carrier: {quote.Carrier}");
                Console.WriteLine($"Price: {quote.Price:F2}");
                Console.WriteLine($"Time: {quote.ElapsedMs} ms");

                Console.WriteLine(
                    $"Cancelled Carriers: " +
                    $"{(quote.CancelledCarriers.Count > 0
                        ? string.Join(", ", quote.CancelledCarriers)
                        : "None")}");
            }

            Console.WriteLine(
                "\nTesting Order #6 - All carriers fail...\n");

            var order6 = orders.FirstOrDefault(
                o => o.Id == 6);

            if (order6 == null)
            {
                Console.WriteLine("Order #6 not found.");
                return;
            }

            try
            {
                var quote =
                    await _shippingQuoteService.GetFastestQuoteAsync(
                        order6, ct);

                Console.WriteLine($"Order: #{order6.Id}");
                Console.WriteLine($"Winning Carrier: {quote.Carrier}");
                Console.WriteLine($"Price: {quote.Price:F2}");
                Console.WriteLine($"Time: {quote.ElapsedMs} ms");
            }
            catch (ShippingProviderUnavailableException ex)
            {
                Console.WriteLine(
                    $"Order #6 Error: {ex.Message}");
            }
        }

        // Task 10.4 A - Fraud Scoring
        public async Task RunTask10_4AAsync(CancellationToken ct)
        {
            Console.WriteLine(
                "\n========== TASK 10.4 A - FRAUD SCORING ==========\n");

            const int totalOrders = 10_000;

            // Create orders in memory only.
            var orderIds = Enumerable.Range(1, totalOrders).ToList();

            // R1 - Sequential scoring
            var sequentialFlaggedIds = new List<int>();

            var sequentialStopwatch = Stopwatch.StartNew();

            foreach (var orderId in orderIds)
            {
                ct.ThrowIfCancellationRequested();

                int score = Score(orderId);

                if (score >= 90)
                {
                    sequentialFlaggedIds.Add(orderId);
                }
            }

            sequentialStopwatch.Stop();

            // R2 - Parallel scoring
            var parallelFlaggedIds = new ConcurrentBag<int>();

            var usedThreads = new ConcurrentDictionary<int, byte>();

            var parallelStopwatch = Stopwatch.StartNew();

            await Task.Run(() =>
            {
                Parallel.ForEach(
                    orderIds,
                    new ParallelOptions
                    {
                        MaxDegreeOfParallelism = Environment.ProcessorCount,
                        CancellationToken = ct
                    },
                    orderId =>
                    {
                        ct.ThrowIfCancellationRequested();

                        int threadId =
                            Environment.CurrentManagedThreadId;

                        usedThreads.TryAdd(threadId, 0);

                        int score = Score(orderId);

                        if (score >= 90)
                        {
                            parallelFlaggedIds.Add(orderId);
                        }
                    });
            }, ct);

            parallelStopwatch.Stop();

            // Compare results
            var parallelResults = parallelFlaggedIds
                .OrderBy(id => id)
                .ToList();

            bool sameResults =
                sequentialFlaggedIds.SequenceEqual(parallelResults);

            double speedUp =
                parallelStopwatch.Elapsed.TotalMilliseconds > 0
                    ? sequentialStopwatch.Elapsed.TotalMilliseconds /
                      parallelStopwatch.Elapsed.TotalMilliseconds
                    : 0;

            // Print summary
            Console.WriteLine(
                "\n========== FRAUD SCORING SUMMARY ==========");

            Console.WriteLine(
                $"Sequential Flagged: {sequentialFlaggedIds.Count}");

            Console.WriteLine(
                $"Parallel Flagged: {parallelResults.Count}");

            Console.WriteLine(
                $"First 5 Flagged IDs: " +
                $"{string.Join(", ", parallelResults.Take(5))}");

            Console.WriteLine(
                $"Same Results: {sameResults}");

            Console.WriteLine(
                $"Sequential Time: " +
                $"{sequentialStopwatch.Elapsed.TotalSeconds:F2} seconds");

            Console.WriteLine(
                $"Parallel Time: " +
                $"{parallelStopwatch.Elapsed.TotalSeconds:F2} seconds");

            Console.WriteLine(
                $"Speed-up: {speedUp:F2}x");

            Console.WriteLine(
                $"Distinct Threads Used: {usedThreads.Count}");

            Console.WriteLine(
                $"Processor Count: {Environment.ProcessorCount}");

            // R4 - Cancellation test
            await TestFraudScoringCancellationAsync(orderIds);
        }

        private async Task TestFraudScoringCancellationAsync(
            List<int> orderIds)
        {
            Console.WriteLine(
                "\n========== FRAUD SCORING CANCELLATION TEST ==========\n");

            using var cancellationCts =
                new CancellationTokenSource();

            var firstIterationStarted = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);

            // Catch the expected cancellation inside the Task.Run delegate.
            var cancellationTask = Task.Run(() =>
            {
                try
                {
                    Parallel.ForEach(
                        orderIds,
                        new ParallelOptions
                        {
                            MaxDegreeOfParallelism =
                                Environment.ProcessorCount,

                            CancellationToken = cancellationCts.Token
                        },
                        orderId =>
                        {
                            firstIterationStarted.TrySetResult();

                            Score(orderId);
                        });

                    return false;
                }
                catch (OperationCanceledException)
                    when (cancellationCts.IsCancellationRequested)
                {
                    return true;
                }
            });

            // Wait until parallel processing actually starts.
            await firstIterationStarted.Task;

            // Give parallel workers time to process.
            await Task.Delay(100);

            var stopwatch = Stopwatch.StartNew();

            // Request cancellation.
            cancellationCts.Cancel();

            // This await returns a bool instead of throwing OCE.
            bool cancellationObserved = await cancellationTask;

            stopwatch.Stop();

            Console.WriteLine(
                cancellationObserved
                    ? "Parallel scoring cancelled successfully."
                    : "Cancellation test failed.");

            Console.WriteLine(
                $"Cancellation Response Time: " +
                $"{stopwatch.ElapsedMilliseconds} ms");
        }

        private static int Score(int orderId)
        {
            long h = orderId;

            for (int i = 0; i < 100_000; i++)
            {
                h = (h * 31 + i) % 1_000_000_007;
            }

            return (int)(h % 100);
        }

        // Task 10.4 B - Email Re-send
        public async Task RunTask10_4BAsync(CancellationToken ct)
        {
            Console.WriteLine(
                "\n========== TASK 10.4 B - EMAIL RE-SEND ==========\n");

            var testData = Enumerable.Range(1001, 30)
                .Select(id => (
                    Order: new Order
                    {
                        Id = id,
                        CustomerId = id,
                        Items = new List<OrderItem>(),
                        CreatedAt = DateTime.UtcNow
                    },
                    Customer: new Customer
                    {
                        Id = id,
                        Name = $"Test Customer {id}",
                        Email = $"test{id}@example.com",
                        IsActive = true
                    }))
                .ToList();

            int currentSends = 0;
            int maxConcurrentSends = 0;

            var stopwatch = Stopwatch.StartNew();

            await Parallel.ForEachAsync(
                testData,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = 4,
                    CancellationToken = ct
                },
                async (item, token) =>
                {
                    int current = Interlocked.Increment(
                        ref currentSends);

                    int observed;

                    do
                    {
                        observed = maxConcurrentSends;

                        if (current <= observed)
                            break;

                    } while (Interlocked.CompareExchange(
                        ref maxConcurrentSends,
                        current,
                        observed) != observed);

                    try
                    {
                        await _emailService.SendConfirmationAsync(
                            item.Order,
                            item.Customer,
                            token);
                    }
                    finally
                    {
                        Interlocked.Decrement(ref currentSends);
                    }
                });

            stopwatch.Stop();

            var emails = await _emailService.GetAllEmailsAsync(ct);

            var testEmailRecords = emails
                .Where(e => e.OrderId >= 1001 && e.OrderId <= 1030)
                .ToList();

            bool allSentExactlyOnce =
                testEmailRecords.Count == 30 &&
                Enumerable.Range(1001, 30).All(id =>
                    testEmailRecords.Count(e => e.OrderId == id) == 1);

            Console.WriteLine(
                "\n========== EMAIL RE-SEND SUMMARY ==========");

            Console.WriteLine(
                $"Emails Sent: {testEmailRecords.Count}");

            Console.WriteLine(
                $"Max Concurrent Sends: {maxConcurrentSends}");

            Console.WriteLine(
                $"Total Time: {stopwatch.Elapsed.TotalSeconds:F2} seconds");

            Console.WriteLine(
                $"All 30 Emails Saved Exactly Once: {allSentExactlyOnce}");
        }

        private record OrderResult(
            Order Order,
            double ElapsedSeconds);
    }
}
