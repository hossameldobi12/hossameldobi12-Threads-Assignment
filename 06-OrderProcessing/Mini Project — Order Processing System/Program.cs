
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using FileStorageDesign.Constants;
using FileStorageDesign.Interfaces;
using FileStorageDesign.Models;
using FileStorageDesign.Repositories;
using FileStorageDesign.Runners;
using FileStorageDesign.Services;
using FileStorageDesign.Shipping;
using FileStorageDesign.Storage;
using OrderProcessingSystem.Storage;
using Mini_Project___Order_Processing_System.Runners;

internal class Program
{
    static async Task Main(string[] args)
    {
        string? phase = null;
        int? cancelAfter = null;

        // Parse command-line arguments
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Equals("--cancel-after", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length ||
                    !int.TryParse(args[i + 1], out int milliseconds) ||
                    milliseconds <= 0)
                {
                    Console.WriteLine("Invalid --cancel-after value.");
                    Console.WriteLine("Example: dotnet run -- phase4 --cancel-after 5000");
                    return;
                }

                cancelAfter = milliseconds;
                i++;
            }
            else if (phase == null)
            {
                phase = args[i].ToLowerInvariant();
            }
            else
            {
                Console.WriteLine($"Unknown argument: {args[i]}");
                return;
            }
        }

        // Show menu when no phase is provided
        if (phase == null)
        {
            phase = ShowMenu();
        }

        if (phase == null)
            return;

        if (phase is not ("phase1" or "phase2" or "phase3" or "phase4" or "all"))
        {
            Console.WriteLine($"Unknown phase: {phase}");
            ShowUsage();
            return;
        }

        using var cts = new CancellationTokenSource();

        if (cancelAfter.HasValue)
        {
            cts.CancelAfter(cancelAfter.Value);

            Console.WriteLine(
                $"Cancellation scheduled after {cancelAfter.Value} ms.");
        }

        try
        {
            await RunPhaseAsync(phase, cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine();
            Console.WriteLine("Operation was cancelled.");
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"Unexpected error: {ex.Message}");
            Console.WriteLine(ex);
        }
    }

    private static async Task RunPhaseAsync(
        string phase,
        CancellationToken ct)
    {
        // Shared infrastructure
        var jsonFileStore = new JsonFileStore();
        var dataSeeder = new DataSeeder(jsonFileStore);

        // Repositories
        var customerRepository = new CustomerRepository(jsonFileStore);
        var productRepository = new ProductRepository(jsonFileStore);
        var couponRepository = new CouponRepository(jsonFileStore);
        var incomingOrderRepository = new IncomingOrderRepository(jsonFileStore);

        var orderRepository = new OrderRepository(
            jsonFileStore,
            Array.Empty<int>());

        // Sync repositories
        var syncCustomerRepository = new SyncCustomerRepository(jsonFileStore);
        var syncProductRepository = new SyncProductRepository(jsonFileStore);
        var syncCouponRepository = new SyncCouponRepository(jsonFileStore);

        // Async services
        var customerService = new CustomerService(customerRepository);
        var inventoryService = new InventoryService(productRepository);

        var pricingService = new PricingService(
            productRepository,
            couponRepository,
            customerRepository);

        var paymentGateway = new PaymentGateway(
            jsonFileStore,
            Array.Empty<int>(),
            Array.Empty<int>());

        var emailService = new EmailService(
            jsonFileStore,
            new Dictionary<int, int>());

        var orderProcessor = new OrderProcessor(
            customerService,
            inventoryService,
            pricingService,
            paymentGateway,
            orderRepository,
            emailService,
            jsonFileStore);

        // Phase 1
        var syncOrderValidator = new SyncOrderValidator(
            syncCouponRepository,
            syncCustomerRepository,
            syncProductRepository);

        var asyncOrderValidator = new OrderValidatorService(
            couponRepository,
            customerRepository,
            productRepository,
            orderRepository);

        var phase1Runner = new Phase1Runner(
            dataSeeder,
            incomingOrderRepository,
            syncOrderValidator,
            asyncOrderValidator,
            orderRepository);

        // Phase 2
        var orderPricerService = new OrderPricerService(pricingService);

        var raceConditionDemo = new RaceConditionDemo(
            productRepository,
            jsonFileStore);

        var invoiceGenerator = new InvoiceGenerator();

        var orderProcessorV2 = new OrderProcessorV2(
            productRepository,
            orderRepository,
            orderPricerService,
            invoiceGenerator);

        var reportGenerator = new ReportGenerator(
            orderRepository,
            productRepository);

        var phase2Runner = new Phase2Runner(
            incomingOrderRepository,
            orderPricerService,
            dataSeeder,
            raceConditionDemo,
            asyncOrderValidator,
            orderProcessorV2,
            productRepository,
            orderRepository,
            reportGenerator);

        // Phase 3
        var syncInventoryService = new SyncInventoryService(productRepository);

        var syncPricingService = new SyncPricingService(
            productRepository,
            couponRepository,
            customerRepository);

        var syncPaymentGateway = new SyncPaymentGateway(
            jsonFileStore,
            Array.Empty<int>(),
            Array.Empty<int>());

        var syncEmailService = new SyncEmailService(
            jsonFileStore,
            new Dictionary<int, int>());

        var syncOrderProcessor = new SyncOrderProcessor(
            customerRepository,
            syncInventoryService,
            syncPricingService,
            syncPaymentGateway,
            orderRepository,
            syncEmailService,
            jsonFileStore);

        var fireAndForgetInvestigation = new FireAndForgetInvestigation(
            orderRepository,
            emailService);

        var customerServiceForPhase3 = new CustomerService(customerRepository);

        var phase3Runner = new Phase3Runner(
            dataSeeder,
            incomingOrderRepository,
            syncOrderProcessor,
            orderProcessor,
            jsonFileStore,
            fireAndForgetInvestigation,
            customerServiceForPhase3);

        // Phase 4
        var customerDashboardService = new CustomerDashboardService(
            customerService,
            orderRepository,
            paymentGateway,
            emailService,
            failPaymentsForCustomerIds: new[] { 99 });

        var fastShip = new FastShip(productRepository);
        var cairoExpress = new CairoExpress(productRepository);
        var nileCourier = new NileCourier(productRepository);

        var shippingQuoteService = new ShippingQuoteService(
            new IShippingCarrier[]
            {
                fastShip,
                cairoExpress,
                nileCourier
            });

        var phase4Runner = new Phase4Runner(
            dataSeeder,
            incomingOrderRepository,
            orderProcessor,
            jsonFileStore,
            customerDashboardService,
            shippingQuoteService,
            emailService);

        // Run selected phase
        switch (phase)
        {
            case "phase1":
                await phase1Runner.RunAsync(ct);
                break;

            case "phase2":
                await phase2Runner.RunAsync(ct);
                break;

            case "phase3":
                await phase3Runner.RunAsync(ct);
                break;

            case "phase4":
                await RunPhase4Async(phase4Runner, ct);
                break;

            case "all":
                Console.WriteLine("\n========== RUNNING ALL PHASES ==========");

                Console.WriteLine("\n========== PHASE 1 ==========");
                await phase1Runner.RunAsync(ct);

                Console.WriteLine("\n========== PHASE 2 ==========");
                await phase2Runner.RunAsync(ct);

                Console.WriteLine("\n========== PHASE 3 ==========");
                await phase3Runner.RunAsync(ct);

                Console.WriteLine("\n========== PHASE 4 ==========");
                await RunPhase4Async(phase4Runner, ct);

                Console.WriteLine("\n========== ALL PHASES COMPLETED ==========");
                break;
        }
    }

    private static async Task RunPhase4Async(
        Phase4Runner runner,
        CancellationToken ct)
    {
        await runner.RunTask10_1Async(ct);
        await runner.RunTask10_2Async(ct);
        await runner.RunTask10_3Async(ct);
        await runner.RunTask10_4AAsync(ct);
        await runner.RunTask10_4BAsync(ct);
    }

    private static string? ShowMenu()
    {
        Console.WriteLine();
        Console.WriteLine("========== ORDER PROCESSING SYSTEM ==========");
        Console.WriteLine("1. Phase 1");
        Console.WriteLine("2. Phase 2");
        Console.WriteLine("3. Phase 3");
        Console.WriteLine("4. Phase 4");
        Console.WriteLine("5. Run All Phases");
        Console.WriteLine("0. Exit");
        Console.WriteLine("=============================================");
        Console.Write("Choose an option: ");

        string? choice = Console.ReadLine();

        return choice switch
        {
            "1" => "phase1",
            "2" => "phase2",
            "3" => "phase3",
            "4" => "phase4",
            "5" => "all",
            _ => null
        };
    }

    private static void ShowUsage()
    {
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("dotnet run -- phase1");
        Console.WriteLine("dotnet run -- phase2");
        Console.WriteLine("dotnet run -- phase3");
        Console.WriteLine("dotnet run -- phase4");
        Console.WriteLine("dotnet run -- all");
        Console.WriteLine();
        Console.WriteLine("With cancellation:");
        Console.WriteLine("dotnet run -- phase4 --cancel-after 5000");
    }
}
