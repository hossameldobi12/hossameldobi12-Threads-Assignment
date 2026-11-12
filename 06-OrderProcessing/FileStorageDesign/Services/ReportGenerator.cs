
using FileStorageDesign.Models;
using FileStorageDesign.Repositories;
using FileStorageDesign.Helpers;
using System.Text.Json;

namespace FileStorageDesign.Services
{
    public class ReportGenerator
    {
        private readonly OrderRepository _orderRepository;
        private readonly ProductRepository _productRepository;
        private readonly string _dataDirectory;

        public ReportGenerator(
            OrderRepository orderRepository,
            ProductRepository productRepository)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;

            _dataDirectory = Path.Combine(
                AppContext.BaseDirectory, "Data");
        }

        public async Task<Report> GenerateAsync(
            CancellationToken ct = default)
        {
            // Load orders and products concurrently
            var ordersTask = _orderRepository.GetAllOrderAsync(ct);
            var productsTask = _productRepository.GetAllProductsAsync(ct);

            await Task.WhenAll(ordersTask, productsTask);

            var orders = await ordersTask;
            var products = await productsTask;

            var completedOrders = orders
                .Where(o => o.Status == OrderStatus.Completed)
                .ToList();

            var completedCount = completedOrders.Count;

            // Calculate revenue
            decimal totalRevenue = completedOrders.Sum(
                o => o.TotalAmount);

            // Calculate average order value
            decimal averageOrderValue = completedCount == 0
                ? 0
                : totalRevenue / completedCount;

            // Calculate top selling product
            var topSelling = completedOrders
                .SelectMany(o => o.Items)
                .GroupBy(item => item.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Quantity = g.Sum(item => item.Quantity)
                })
                .OrderByDescending(x => x.Quantity)
                .ThenBy(x => x.ProductId)
                .FirstOrDefault();

            var topSellingProduct = topSelling == null
                ? null
                : products.FirstOrDefault(
                    p => p.Id == topSelling.ProductId);

            var generatedAt = DateTime.Now;

            var report = new Report
            {
                TotalOrders = orders.Count,

                CompletedCount = completedCount,
                RejectedCount = orders.Count(
                    o => o.Status == OrderStatus.Rejected),
                FailedCount = orders.Count(
                    o => o.Status == OrderStatus.Failed),
                PaymentFailedCount = orders.Count(
                    o => o.Status == OrderStatus.PaymentFailed),

                TotalRevenue = totalRevenue,
                AverageOrderValue = averageOrderValue,

                TopSellingProduct = topSellingProduct?.Name
                    ?? string.Empty,

                TopSellingQuantity = topSelling?.Quantity ?? 0,

                GeneratedAt = generatedAt
            };

            // Save report
            var reportsDirectory = Path.Combine(
                _dataDirectory, "reports");

            Directory.CreateDirectory(reportsDirectory);

            var fileName = $"report_{generatedAt:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine(reportsDirectory, fileName);

            var json = JsonSerializer.Serialize(
                report, JsonOptions.Default);

            await File.WriteAllTextAsync(filePath, json, ct);

            return report;
        }
    }
}
