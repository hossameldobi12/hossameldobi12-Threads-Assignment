
using FileStorageDesign.Constants;
using FileStorageDesign.Models;
using FileStorageDesign.Repositories;
using FileStorageDesign.Services;
using FileStorageDesign.Storage;
using OrderProcessingSystem.Storage;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace FileStorageDesign.Runners
{
    public class Phase3Runner
    {
        private readonly DataSeeder _dataSeeder;
        private readonly IncomingOrderRepository _incomingOrderRepository;
        private readonly SyncOrderProcessor _syncOrderProcessor;
        private readonly OrderProcessor _asyncOrderProcessor;
        private readonly JsonFileStore _jsonFileStore;
        private readonly FireAndForgetInvestigation _fireAndForgetInvestigation;
        private readonly CustomerService _customerService;

        public Phase3Runner(
            DataSeeder dataSeeder,
            IncomingOrderRepository incomingOrderRepository,
            SyncOrderProcessor syncOrderProcessor,
            OrderProcessor asyncOrderProcessor,
            JsonFileStore jsonFileStore,
            FireAndForgetInvestigation fireAndForgetInvestigation,
            CustomerService customerService)
        {
            _dataSeeder = dataSeeder;
            _incomingOrderRepository = incomingOrderRepository;
            _syncOrderProcessor = syncOrderProcessor;
            _asyncOrderProcessor = asyncOrderProcessor;
            _jsonFileStore = jsonFileStore;
            _fireAndForgetInvestigation = fireAndForgetInvestigation;
            _customerService = customerService;
        }

        public async Task RunAsync(CancellationToken ct)
        {
            // =========================
            // Sync Version
            // =========================

            Console.WriteLine("\n========== SYNC VERSION ==========\n");

            await _dataSeeder.ResetAsync(ct);

            var incomingOrders =
                await _incomingOrderRepository.GetAllAsync(ct);

            var orders = incomingOrders.Take(10).ToList();

            var syncThreadIds = new ConcurrentDictionary<int, byte>();
            var syncExceptions = new ConcurrentQueue<Exception>();

            var syncThreads = new List<Thread>();

            var syncStopwatch = Stopwatch.StartNew();

            int threadPoolBeforeSync = ThreadPool.ThreadCount;

            foreach (var order in orders)
            {
                var currentOrder = order;

                var thread = new Thread(() =>
                {
                    syncThreadIds.TryAdd(
                        Thread.CurrentThread.ManagedThreadId, 0);

                    try
                    {
                        _syncOrderProcessor.ProcessOrder(currentOrder);
                    }
                    catch (Exception ex)
                    {
                        syncExceptions.Enqueue(ex);
                    }
                    finally
                    {
                        syncThreadIds.TryAdd(
                            Thread.CurrentThread.ManagedThreadId, 0);
                    }
                });

                syncThreads.Add(thread);
                thread.Start();
            }

            // Wait for all 10 dedicated threads.
            foreach (var thread in syncThreads)
            {
                thread.Join();
            }

            syncStopwatch.Stop();

            int threadPoolAfterSync = ThreadPool.ThreadCount;

            if (!syncExceptions.IsEmpty)
            {
                throw new AggregateException(
                    "One or more sync orders failed.",
                    syncExceptions);
            }

            var syncResults =
                _jsonFileStore.Read<Order>(DataFiles.Orders);

            PrintSummary("SYNC", syncResults);

            Console.WriteLine($"Sync Time: {syncStopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"Distinct Threads: {syncThreadIds.Count}");
            Console.WriteLine($"ThreadPool.ThreadCount Before: {threadPoolBeforeSync}");
            Console.WriteLine($"ThreadPool.ThreadCount After: {threadPoolAfterSync}");

            // =========================
            // Async Version
            // =========================

            Console.WriteLine("\n========== ASYNC VERSION ==========\n");

            await _dataSeeder.ResetAsync(ct);

            incomingOrders =
                await _incomingOrderRepository.GetAllAsync(ct);

            orders = incomingOrders.Take(10).ToList();

            var asyncThreadIds = new ConcurrentDictionary<int, byte>();

            var asyncStopwatch = Stopwatch.StartNew();

            int threadPoolBeforeAsync = ThreadPool.ThreadCount;

            // Start all 10 tasks before awaiting them.
            var tasks = orders.Select(async order =>
            {
                asyncThreadIds.TryAdd(
                    Thread.CurrentThread.ManagedThreadId, 0);

                bool result = await _asyncOrderProcessor.ProcessOrderAsync(
                    order, ct);

                asyncThreadIds.TryAdd(
                    Thread.CurrentThread.ManagedThreadId, 0);

                return result;
            }).ToList();

            // Await each task after all tasks have started.
            foreach (var task in tasks)
            {
                await task;
            }

            asyncStopwatch.Stop();

            int threadPoolAfterAsync = ThreadPool.ThreadCount;

            var asyncResults =
                _jsonFileStore.Read<Order>(DataFiles.Orders);

            PrintSummary("ASYNC", asyncResults);

            Console.WriteLine($"Async Time: {asyncStopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"Distinct Threads (sampled): {asyncThreadIds.Count}");
            Console.WriteLine($"ThreadPool.ThreadCount Before: {threadPoolBeforeAsync}");
            Console.WriteLine($"ThreadPool.ThreadCount After: {threadPoolAfterAsync}");

            // =========================
            // Fire-and-forget Investigation
            // =========================

            await RunFireAndForgetInvestigationAsync(ct);
        }

        private async Task RunFireAndForgetInvestigationAsync(
            CancellationToken ct)
        {
            Console.WriteLine(
                "\n========== FIRE-AND-FORGET INVESTIGATION ==========\n");

            await _dataSeeder.ResetAsync(ct);

            var incomingOrders =
                await _incomingOrderRepository.GetAllAsync(ct);

            var order = incomingOrders.First(o => o.Id == 1);

            var customer =
                await _customerService.GetCustomerAsync(
                    order.CustomerId, ct);

            Console.WriteLine("Starting order processing...");

            await _fireAndForgetInvestigation.ProcessOrderAsync(
                order, customer);

            Console.WriteLine(
                "Runner continues after ProcessOrderAsync returns.");
        }

        private static void PrintSummary(
            string version,
            List<Order> orders)
        {
            Console.WriteLine($"\n===== {version} SUMMARY =====");

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
    }
}
