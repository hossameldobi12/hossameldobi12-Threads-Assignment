using FileStorageDesign.Repositories;
using FileStorageDesign.Runners;
using FileStorageDesign.Services;
using FileStorageDesign.Storage;
using Mini_Project___Order_Processing_System.Runners;
using OrderProcessingSystem.Storage;

var jsonFileStore = new JsonFileStore();

//// Storage
var dataSeeder = new DataSeeder(jsonFileStore);

// Async Repositories
var customerRepository = new CustomerRepository(jsonFileStore);

var productRepository = new ProductRepository(jsonFileStore);

var couponRepository = new CouponRepository(jsonFileStore);

var orderRepository = new OrderRepository(jsonFileStore);

var incomingOrderRepository = new IncomingOrderRepository(jsonFileStore);

var raceConditionDemo = new RaceConditionDemo(productRepository, jsonFileStore);

var invoiceGenerator = new InvoiceGenerator();

var orderValidatorService = new OrderValidatorService(
    couponRepository,
    customerRepository,
    productRepository,
    orderRepository);

var orderProcessor = new OrderProcessorV2(
    productRepository,
    orderRepository,
    new OrderPricerService(new PricingService(productRepository, couponRepository, customerRepository, new[] { 7 })),
    new InvoiceGenerator());

var reportGenerator = new ReportGenerator(orderRepository, productRepository);


//// Sync Repositories
//var syncCustomerRepository = new SyncCustomerRepository(jsonFileStore);
//var syncProductRepository = new SyncProductRepository(jsonFileStore);
//var syncCouponRepository = new SyncCouponRepository(jsonFileStore);

//// Validators
//var asyncOrderValidator = new OrderValidatorService(
//    couponRepository,
//    customerRepository,
//    productRepository,
//    orderRepository);

//var syncOrderValidator = new SyncOrderValidator(
//    syncCouponRepository,
//    syncCustomerRepository,
//    syncProductRepository);

//// Runner
//var phase1Runner = new Phase1Runner(
//    dataSeeder,
//    incomingOrderRepository,
//    syncOrderValidator,
//    asyncOrderValidator,
//    orderRepository);

//// Run
//await phase1Runner.RunAsync(CancellationToken.None);


var pricingService = new PricingService(
    productRepository,
    couponRepository,
    customerRepository,new [] {7});

var orderPricerService = new OrderPricerService(pricingService); 
var phase2Runner = new Phase2Runner(
    incomingOrderRepository,
    orderPricerService,
    dataSeeder,raceConditionDemo,
    orderValidatorService,
    orderProcessor,
    productRepository,
    orderRepository,
    reportGenerator);


await phase2Runner.RunAsync(CancellationToken.None);



