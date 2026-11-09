using FileStorageDesign.Constants;
using FileStorageDesign.Models;
using OrderProcessingSystem.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileStorageDesign.Storage
{
    public sealed class DataSeeder
    {
        private readonly JsonFileStore _jsonFileStore;
        private readonly string _dataDirectory;

        public DataSeeder(JsonFileStore jsonFileStore)
        {
            _jsonFileStore = jsonFileStore;
            _dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
        }
        public async Task ResetAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Directory.Exists(_dataDirectory))
            {
                Directory.Delete(_dataDirectory, recursive: true);
            }
            Directory.CreateDirectory(_dataDirectory);
            await ApplySeedDataAsync(cancellationToken);


        }

        private List<Customer> GetCustomers()
        {
            return new List<Customer>
    {
        new Customer { Id = 1, Name = "Ahmed Ali", Email = "ahmed.ali@example.com", IsActive = true, LoyaltyTier = LoyaltyTier.Gold },
        new Customer { Id = 2, Name = "Sara Mohamed", Email = "sara.mohamed@example.com", IsActive = true, LoyaltyTier = LoyaltyTier.Silver },
        new Customer { Id = 3, Name = "Omar Hassan", Email = "omar.hassan@example.com", IsActive = true, LoyaltyTier = LoyaltyTier.Bronze },
        new Customer { Id = 4, Name = "Mona Ibrahim", Email = "mona.ibrahim@example.com", IsActive = false, LoyaltyTier = LoyaltyTier.Silver },
        new Customer { Id = 5, Name = "Youssef Adel", Email = "youssef.adel@example.com", IsActive = true, LoyaltyTier = LoyaltyTier.Gold },
        new Customer { Id = 6, Name = "Nour Khaled", Email = "nour.khaled@example.com", IsActive = true, LoyaltyTier = LoyaltyTier.Bronze },
        new Customer { Id = 7, Name = "Karim Samir", Email = "karim.samir@example.com", IsActive = true, LoyaltyTier = LoyaltyTier.Silver },
        new Customer { Id = 8, Name = "Laila Mahmoud", Email = "laila.mahmoud@example.com", IsActive = true, LoyaltyTier = LoyaltyTier.Gold },
        new Customer { Id = 9, Name = "Hassan Tarek", Email = "hassan.tarek@example.com", IsActive = true, LoyaltyTier = LoyaltyTier.Bronze },
        new Customer { Id = 10, Name = "Dina Fathy", Email = "dina.fathy@example.com", IsActive = true, LoyaltyTier = LoyaltyTier.Silver }
    };
        }

        private List<Product> GetProducts()
        {
            return new List<Product>
    {
        new Product { Id = 1, Name = "Laptop", Price = 1200.00m, StockQuantity = 10, WeightKg = 2.2m },
        new Product { Id = 2, Name = "Smartphone", Price = 800.00m, StockQuantity = 15, WeightKg = 0.4m },
        new Product { Id = 3, Name = "Wireless Mouse", Price = 25.00m, StockQuantity = 2, WeightKg = 0.1m },
        new Product { Id = 4, Name = "Mechanical Keyboard", Price = 90.00m, StockQuantity = 1, WeightKg = 0.9m },
        new Product { Id = 5, Name = "USB-C Hub", Price = 45.00m, StockQuantity = 30, WeightKg = 0.2m },
        new Product { Id = 6, Name = "Headphones", Price = 150.00m, StockQuantity = 12, WeightKg = 0.3m },
        new Product { Id = 7, Name = "27\" Monitor", Price = 350.00m, StockQuantity = 6, WeightKg = 5.0m },
        new Product { Id = 8, Name = "Webcam", Price = 70.00m, StockQuantity = 20, WeightKg = 0.2m }
    };
        }

        private List<Coupon> GetCoupons()
        {
            return new List<Coupon>
    {
        new Coupon { Code = "SAVE10", DiscountPercent = 10, ExpiryDate = DateTime.Today.AddDays(180) },
        new Coupon { Code = "WELCOME15", DiscountPercent = 15, ExpiryDate = DateTime.Today.AddDays(90) },
        new Coupon { Code = "FLASH20", DiscountPercent = 20, ExpiryDate = DateTime.Today.AddDays(7) },
        new Coupon { Code = "OLD20", DiscountPercent = 20, ExpiryDate = DateTime.Today.AddDays(-30) }
    };
        }

        private List<Order> GetIncomingOrders()
        {
            return new List<Order>
    {
        new Order { Id = 1, CustomerId = 1, Items = new List<OrderItem> { new OrderItem { ProductId = 1, Quantity = 1, UnitPrice = 0m }, new OrderItem { ProductId = 3, Quantity = 1, UnitPrice = 0m } }, CouponCode = "SAVE10" },
        new Order { Id = 2, CustomerId = 2, Items = new List<OrderItem> { new OrderItem { ProductId = 2, Quantity = 1, UnitPrice = 0m } } },
        new Order { Id = 3, CustomerId = 3, Items = new List<OrderItem> { new OrderItem { ProductId = 5, Quantity = 2, UnitPrice = 0m }, new OrderItem { ProductId = 8, Quantity = 1, UnitPrice = 0m } }, CouponCode = "WELCOME15" },
        new Order { Id = 4, CustomerId = 4, Items = new List<OrderItem> { new OrderItem { ProductId = 6, Quantity = 1, UnitPrice = 0m } } },
        new Order { Id = 5, CustomerId = 5, Items = new List<OrderItem> { new OrderItem { ProductId = 7, Quantity = 1, UnitPrice = 0m } } },
        new Order { Id = 6, CustomerId = 6, Items = new List<OrderItem> { new OrderItem { ProductId = 5, Quantity = 1, UnitPrice = 0m } }, CouponCode = "FLASH20" },
        new Order { Id = 7, CustomerId = 7, Items = new List<OrderItem> { new OrderItem { ProductId = 6, Quantity = 1, UnitPrice = 0m }, new OrderItem { ProductId = 8, Quantity = 1, UnitPrice = 0m } } },
        new Order { Id = 8, CustomerId = 8, Items = new List<OrderItem> { new OrderItem { ProductId = 1, Quantity = 1, UnitPrice = 0m } }, CouponCode = "SAVE10" },
        new Order { Id = 9, CustomerId = 9, Items = new List<OrderItem> { new OrderItem { ProductId = 3, Quantity = 5, UnitPrice = 0m } } },
        new Order { Id = 10, CustomerId = 10, Items = new List<OrderItem> { new OrderItem { ProductId = 4, Quantity = 1, UnitPrice = 0m } } },
        new Order { Id = 11, CustomerId = 1, Items = new List<OrderItem> { new OrderItem { ProductId = 4, Quantity = 1, UnitPrice = 0m } } },
        new Order { Id = 12, CustomerId = 2, Items = new List<OrderItem> { new OrderItem { ProductId = 2, Quantity = 1, UnitPrice = 0m } }, CouponCode = "OLD20" },
        new Order { Id = 13, CustomerId = 3, Items = new List<OrderItem> { new OrderItem { ProductId = 6, Quantity = 2, UnitPrice = 0m } } },
        new Order { Id = 14, CustomerId = 5, Items = new List<OrderItem> { new OrderItem { ProductId = 8, Quantity = 2, UnitPrice = 0m }, new OrderItem { ProductId = 5, Quantity = 1, UnitPrice = 0m } }, CouponCode = "WELCOME15" },
        new Order { Id = 15, CustomerId = 99, Items = new List<OrderItem> { new OrderItem { ProductId = 2, Quantity = 1, UnitPrice = 0m } } },
        new Order { Id = 16, CustomerId = 6, Items = new List<OrderItem> { new OrderItem { ProductId = 7, Quantity = 1, UnitPrice = 0m }, new OrderItem { ProductId = 5, Quantity = 1, UnitPrice = 0m } } },
        new Order { Id = 17, CustomerId = 7, Items = new List<OrderItem> { new OrderItem { ProductId = 2, Quantity = 1, UnitPrice = 0m } }, CouponCode = "SAVE10" },
        new Order { Id = 18, CustomerId = 8, Items = new List<OrderItem> { new OrderItem { ProductId = 6, Quantity = 1, UnitPrice = 0m } }, CouponCode = "FAKE50" },
        new Order { Id = 19, CustomerId = 9, Items = new List<OrderItem> { new OrderItem { ProductId = 8, Quantity = 1, UnitPrice = 0m } } },
        new Order { Id = 20, CustomerId = 10, Items = new List<OrderItem> { new OrderItem { ProductId = 1, Quantity = 1, UnitPrice = 0m }, new OrderItem { ProductId = 6, Quantity = 1, UnitPrice = 0m } }, CouponCode = "FLASH20" }
    }
            .Select(order =>
            {
                order.Status = OrderStatus.Pending;
                order.CreatedAt = DateTime.Now;
                order.SubTotal = 0m;
                order.Discount = 0m;
                order.ShippingCost = 0m;
                order.TotalAmount = 0m;
                order.FailureReason = null;
                order.TransactionId = null;

                return order;
            })
            .ToList();
        }

        private async Task SeedDataAsync<T>(string fileName, List<T> data, CancellationToken cancellationToken)
        {
            await _jsonFileStore.WriteAsync(fileName, data, cancellationToken);
            Console.WriteLine($"[Seeder] {fileName} -> {data.Count} records");
        }
        private async Task ApplySeedDataAsync(CancellationToken cancellationToken)
        {
            await SeedDataAsync(DataFiles.Customers, GetCustomers(), cancellationToken);
            await SeedDataAsync(DataFiles.Products, GetProducts(), cancellationToken);
            await SeedDataAsync(DataFiles.Coupons, GetCoupons(), cancellationToken);
            await SeedDataAsync(DataFiles.IncomingOrders, GetIncomingOrders(), cancellationToken);

            await SeedDataAsync<Order>(DataFiles.Orders, new List<Order>(), cancellationToken);
            await SeedDataAsync<PaymentTransaction>(DataFiles.Payments, new List<PaymentTransaction>(), cancellationToken);
            await SeedDataAsync<EmailMessage>(DataFiles.Emails, new List<EmailMessage>(), cancellationToken);
            await SeedDataAsync<EmailMessage>(DataFiles.EmailRetry, new List<EmailMessage>(), cancellationToken);

        }
    }
}
