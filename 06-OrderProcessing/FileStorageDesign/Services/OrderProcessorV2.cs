using FileStorageDesign.Models;
using FileStorageDesign.Repositories;
using FileStorageDesign.Services;

namespace FileStorageDesign.Services
{
    public class OrderProcessorV2
    {
        private readonly ProductRepository _productRepository;
        private readonly OrderRepository _orderRepository;
        private readonly OrderPricerService _orderPricer;
        private readonly InvoiceGenerator _invoiceGenerator;

        public OrderProcessorV2(
            ProductRepository productRepository,
            OrderRepository orderRepository,
            OrderPricerService orderPricer,
            InvoiceGenerator invoiceGenerator
          )
        {
            _productRepository = productRepository;
            _orderRepository = orderRepository;
            _orderPricer = orderPricer;
            _invoiceGenerator = invoiceGenerator;
        }
        public async Task<bool> ProcessValidatedOrderAsync(Order order, CancellationToken ct)
        {
            var reservedItems = new List<OrderItem>();

            foreach (var item in order.Items)
            {
                var reserved = await _productRepository.ReserveStockAsync(item.ProductId, item.Quantity, ct);

                if (!reserved)
                {
                    foreach (var reservedItem in reservedItems)
                    {
                        await _productRepository.ReleaseStockAsync(
                            reservedItem.ProductId,
                            reservedItem.Quantity,
                            ct);
                    }

                    var product = await _productRepository.GetProductByIdAsync(
                        item.ProductId,
                        ct);

                    order.Status = OrderStatus.Rejected;
                    order.FailureReason =
                        $"Insufficient stock for '{product?.Name}'";

                    await _orderRepository.SaveOrderAsync(order, ct);

                    return false;
                }

                reservedItems.Add(item);
            }

            await _orderPricer.PriceOrderAsync(order, ct);

            await _invoiceGenerator.GenerateAsync(order, ct);

            order.Status = OrderStatus.Completed;

            await _orderRepository.SaveOrderAsync(order, ct);

            return true;
        }
    }
}