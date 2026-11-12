
using FileStorageDesign.Exceptions;
using FileStorageDesign.logging;
using FileStorageDesign.Models;
using FileStorageDesign.Repositories;

namespace FileStorageDesign.Services
{
    public class PricingService
    {
        private readonly ProductRepository _productRepository;
        private readonly CouponRepository _couponRepository;
        private readonly CustomerRepository _customerRepository;

        public PricingService(
            ProductRepository productRepository,
            CouponRepository couponRepository,
            CustomerRepository customerRepository)
        {
            _productRepository = productRepository;
            _couponRepository = couponRepository;
            _customerRepository = customerRepository;
        }

        public async Task<decimal> CalculateSubTotalAsync(
            Order order,
            CancellationToken ct)
        {
            decimal subTotal = 0;

            var products = await _productRepository.GetAllProductsAsync(ct);

            foreach (var item in order.Items)
            {
                var product = products.FirstOrDefault(p => p.Id == item.ProductId);

                if (product == null)
                    throw new Exception($"Product with ID {item.ProductId} not found.");

                item.UnitPrice = product.Price;
                subTotal += item.UnitPrice * item.Quantity;
            }

            return Math.Round(subTotal, 2, MidpointRounding.AwayFromZero);
        }

        public async Task<decimal> GetDiscountAsync(
            Order order,
            CancellationToken ct)
        {
            var customer = await _customerRepository.GetCunstomerByIdAsync(
                order.CustomerId, ct);

            if (customer == null)
                throw new CustomerNotFoundException($"Customer #{order.CustomerId} not found");

            decimal discountPercent = customer.LoyaltyTier switch
            {
                LoyaltyTier.Gold => 10,
                LoyaltyTier.Silver => 5,
                LoyaltyTier.Bronze => 0,
                _ => 0
            };

            if (!string.IsNullOrEmpty(order.CouponCode))
            {
                var coupon = await _couponRepository.GetCouponByCodeAsync(
                    order.CouponCode, ct);

                if (coupon == null)
                    throw new InvalidCouponException(
                        $"Coupon '{order.CouponCode}' does not exist");

                if (coupon.ExpiryDate < DateTime.Today)
                    throw new InvalidCouponException(
                        $"Coupon '{order.CouponCode}' has expired");

                discountPercent += coupon.DiscountPercent;
            }

            discountPercent = Math.Min(discountPercent, 30);

            decimal discount = order.SubTotal * discountPercent / 100;

            return Math.Round(discount, 2, MidpointRounding.AwayFromZero);
        }

        public async Task<decimal> CalculateShippingCostAsync(
            Order order,
            CancellationToken ct)
        {
            Log.Info("Shipping API", $"Starting handshake for Order #{order.Id}");

            await Task.Delay(150, ct);
            await Task.Delay(300, ct);
            await Task.Delay(Random.Shared.Next(500, 1501), ct);

            if (order.SubTotal >= 1000)
                return 0;

            var products = await _productRepository.GetAllProductsAsync(ct);

            decimal totalQuantity = 0;
            decimal totalWeight = 0;

            foreach (var item in order.Items)
            {
                var product = products.FirstOrDefault(p => p.Id == item.ProductId);

                if (product == null)
                    throw new Exception($"Product with ID {item.ProductId} not found.");

                totalQuantity += item.Quantity;
                totalWeight += product.WeightKg * item.Quantity;
            }

            decimal shippingCost = 5 + (1.5m * totalQuantity) + (2m * totalWeight);

            return Math.Round(shippingCost, 2, MidpointRounding.AwayFromZero);
        }

        public async Task<decimal> CalculateTotalAsync(
            Order order,
            CancellationToken ct)
        {
            order.SubTotal = await CalculateSubTotalAsync(order, ct);

            var discountTask = GetDiscountAsync(order, ct);
            var shippingTask = CalculateShippingCostAsync(order, ct);

            await Task.WhenAll(discountTask, shippingTask);

            order.Discount = await discountTask;
            order.ShippingCost = await shippingTask;

            order.TotalAmount = Math.Round(
                order.SubTotal - order.Discount + order.ShippingCost,
                2,
                MidpointRounding.AwayFromZero);

            return order.TotalAmount;
        }
    }
}
