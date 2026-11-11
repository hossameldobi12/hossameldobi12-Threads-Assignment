
using FileStorageDesign.Repositories;
using FileStorageDesign.Services;
using FileStorageDesign.Storage;
using FileStorageDesign.Runners;
using OrderProcessingSystem.Storage;

var cts = new CancellationTokenSource();

// false = Disable all controlled failures
// true  = Enable all controlled failures
bool enableFailures = false;

try
{
    var jsonFileStore = new JsonFileStore();
    var dataSeeder = new DataSeeder(jsonFileStore);

    // Repositories
    var customerRepository = new CustomerRepository(jsonFileStore);
    var productRepository = new ProductRepository(jsonFileStore);
    var couponRepository = new CouponRepository(jsonFileStore);
    var incomingOrderRepository = new IncomingOrderRepository(jsonFileStore);

    var orderRepository = new OrderRepository(
        jsonFileStore,
        enableFailures ? new[] { 13 } : Array.Empty<int>());

    // Async Services
    var customerService = new CustomerService(customerRepository);
    var inventoryService = new InventoryService(productRepository);

    var pricingService = new PricingService(
        productRepository,
        couponRepository  ,  customerRepository);

    IPaymentGateway paymentGateway = new PaymentGateway(
        jsonFileStore,
        enableFailures ? new[] { 8 } : Array.Empty<int>(),
        enableFailures ? new[] { 5 } : Array.Empty<int>());

    IEmailService emailService = new EmailService(
        jsonFileStore,
        enableFailures
            ? new Dictionary<int, int>
            {
                [3] = 2,
                [17] = int.MaxValue
            }
            : new Dictionary<int, int>());

    var orderProcessor = new OrderProcessor(
        customerService,
        inventoryService,
        pricingService,
        paymentGateway,
        orderRepository,
        emailService,
        jsonFileStore);

    // Sync Services
    var syncInventoryService = new SyncInventoryService(productRepository);

    var syncPricingService = new SyncPricingService(
        productRepository,
        couponRepository,
        customerRepository);

    var syncPaymentGateway = new SyncPaymentGateway(
        jsonFileStore,
        enableFailures ? new[] { 8 } : Array.Empty<int>(),
        enableFailures ? new[] { 5 } : Array.Empty<int>());

    var syncEmailService = new SyncEmailService(
        jsonFileStore,
        enableFailures
            ? new Dictionary<int, int>
            {
                [3] = 2,
                [17] = int.MaxValue
            }
            : new Dictionary<int, int>());

    var syncOrderProcessor = new SyncOrderProcessor(
        customerRepository,
        syncInventoryService,
        syncPricingService,
        syncPaymentGateway,
        orderRepository,
        syncEmailService,
        jsonFileStore);

    var fireAndForgetInvestigation =
    new FireAndForgetInvestigation(orderRepository, emailService);

    // Phase 3 Comparison Runner
    var phase3Runner = new Phase3Runner(
        dataSeeder,
        incomingOrderRepository,
        syncOrderProcessor,
        orderProcessor,
        jsonFileStore,
        fireAndForgetInvestigation,
        customerService);

    await phase3Runner.RunAsync(cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation cancelled.");
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex}");
}
