
using FileStorageDesign.Models;
using FileStorageDesign.Repositories;

namespace FileStorageDesign.Services
{
    public class OrderValidatorService
    {
        private readonly CouponRepository _couponRepository;
        private readonly CustomerRepository _customerRepository;
        private readonly ProductRepository _productRepository;
        private readonly OrderRepository _orderRepository;

        public OrderValidatorService(
            CouponRepository couponRepository,
            CustomerRepository customerRepository,
            ProductRepository productRepository,
            OrderRepository orderRepository)
        {
            _couponRepository = couponRepository;
            _customerRepository = customerRepository;
            _productRepository = productRepository;
            _orderRepository = orderRepository;
        }

        public async Task ValidateOrderAsync(Order order, CancellationToken ct)
        {
            // 1. Validate customer
            var failureReason = await ValidateCustomerAsync(order, ct);

            if (failureReason != null)
            {
                await RejectOrderAsync(order, failureReason, ct);
                return;
            }

            // 2. Validate that all products exist
            var products = new List<Product>();

            failureReason = await ValidateProductsExistAsync(order, products, ct);

            if (failureReason != null)
            {
                await RejectOrderAsync(order, failureReason, ct);
                return;
            }

            // 3. Validate stock
            failureReason = ValidateStock(order, products);

            if (failureReason != null)
            {
                await RejectOrderAsync(order, failureReason, ct);
                return;
            }

            // 4. Validate coupon
            failureReason = await ValidateCouponAsync(order, ct);

            if (failureReason != null)
            {
                await RejectOrderAsync(order, failureReason, ct);
                return;
            }

            // All validations passed
            order.Status = OrderStatus.Validated;
            order.FailureReason = null;

            await _orderRepository.SaveOrderAsync(order, ct);
        }

        private async Task<string?> ValidateCustomerAsync(
            Order order,
            CancellationToken ct)
        {
            var customer = await _customerRepository
                .GetCunstomerByIdAsync(order.CustomerId, ct);

            if (customer == null)
                return $"Customer #{order.CustomerId} not found";

            if (!customer.IsActive)
                return "Customer is inactive";

            return null;
        }

        private async Task<string?> ValidateProductsExistAsync(
            Order order,
            List<Product> products,
            CancellationToken ct)
        {
            foreach (var item in order.Items)
            {
                var product = await _productRepository
                    .GetProductByIdAsync(item.ProductId, ct);

                if (product == null)
                    return $"Product #{item.ProductId} not found";

                products.Add(product);
            }

            return null;
        }

        private string? ValidateStock(
            Order order,
            List<Product> products)
        {
            for (int i = 0; i < order.Items.Count; i++)
            {
                var item = order.Items[i];
                var product = products[i];

                if (product.StockQuantity < item.Quantity)
                    return $"Insufficient stock for '{product.Name}'";
            }

            return null;
        }

        private async Task<string?> ValidateCouponAsync(
            Order order,
            CancellationToken ct)
        {
            if (order.CouponCode == null)
                return null;

            var coupon = await _couponRepository
                .GetCouponByCodeAsync(order.CouponCode, ct);

            if (coupon == null)
                return $"Coupon '{order.CouponCode}' does not exist";

            if (coupon.ExpiryDate < DateTime.Today)
                return $"Coupon '{order.CouponCode}' has expired";

            return null;
        }

        private async Task RejectOrderAsync(
            Order order,
            string failureReason,
            CancellationToken ct)
        {
            order.Status = OrderStatus.Rejected;
            order.FailureReason = failureReason;

            await _orderRepository.SaveOrderAsync(order, ct);
        }
    }
}
