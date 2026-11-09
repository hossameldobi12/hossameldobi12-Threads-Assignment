using FileStorageDesign.Models;
using System.Text;

namespace FileStorageDesign.Services
{
    public class InvoiceGenerator
    {
        private readonly string _dataDirectory;

        public InvoiceGenerator()
        {
            _dataDirectory = Path.Combine(
                AppContext.BaseDirectory,
                "Data");
        }

        public async Task GenerateAsync(
            Order order,
            CancellationToken ct)
        {
            // CPU-bound work: simulate invoice rendering
            _ = await Task.Run(() =>
            {
                long checksum = 0;

                for (int i = 0; i < 20_000_000; i++)
                {
                    checksum = (checksum * 31 + i) % 1_000_000_007;

                    if (i % 100_000 == 0)
                        ct.ThrowIfCancellationRequested();
                }

                return checksum;

            }, ct);

            // Prepare invoice content
            var invoice = new StringBuilder();

            invoice.AppendLine($"Invoice for Order #{order.Id}");
            invoice.AppendLine($"Customer ID: {order.CustomerId}");
            invoice.AppendLine("--------------------------------");

            foreach (var item in order.Items)
            {
                invoice.AppendLine(
                    $"Product ID: {item.ProductId}, " +
                    $"Quantity: {item.Quantity}, " +
                    $"Unit Price: {item.UnitPrice}");
            }

            invoice.AppendLine("--------------------------------");
            invoice.AppendLine($"Subtotal: {order.SubTotal}");
            invoice.AppendLine($"Discount: {order.Discount}");
            invoice.AppendLine($"Shipping: {order.ShippingCost}");
            invoice.AppendLine($"Total: {order.TotalAmount}");

            // I/O-bound work: write invoice file asynchronously
            var invoicesDirectory = Path.Combine(
                _dataDirectory,
                "invoices");

            Directory.CreateDirectory(invoicesDirectory);

            var filePath = Path.Combine(
                invoicesDirectory,
                $"invoice_{order.Id}.txt");

            await File.WriteAllTextAsync(
                filePath,
                invoice.ToString(),
                Encoding.UTF8,
                ct);
        }
    }
}