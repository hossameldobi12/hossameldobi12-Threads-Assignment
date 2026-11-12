
using FileStorageDesign.Exceptions;
using FileStorageDesign.Models;
using FileStorageDesign.Repositories;

namespace FileStorageDesign.Services
{
    public class SyncPricingService
    {
        private readonly ProductRepository _productRepository;
        private readonly CouponRepository _couponRepository;
        private readonly CustomerRepository _customerRepository;

        public SyncPricingService(
            ProductRepository productRepository,
            CouponRepository couponRepository,
            CustomerRepository customerRepository)
        {
            _productRepository = productRepository;
            _couponRepository = couponRepository;
            _customerRepository = customerRepository;
        }

        public decimal CalculateSubTotal(Order order)
        {
            decimal subTotal = 0;

            var products = _productRepository.GetAllProducts();

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

        public decimal GetDiscount(Order order)
        {
            var customer = _customerRepository.GetCustomerById(order.CustomerId);

            if (customer == null)
                throw new CustomerNotFoundException(
                    $"Customer #{order.CustomerId} not found");

            decimal discountPercent = customer.LoyaltyTier switch
            {
                LoyaltyTier.Gold => 10,
                LoyaltyTier.Silver => 5,
                LoyaltyTier.Bronze => 0,
                _ => 0
            };

            if (!string.IsNullOrEmpty(order.CouponCode))
            {
                var coupon = _couponRepository.GetCouponByCode(order.CouponCode);

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

        public decimal CalculateShippingCost(Order order)
        {
            Thread.Sleep(150);
            Thread.Sleep(300);
            Thread.Sleep(Random.Shared.Next(500, 1501));

            if (order.SubTotal >= 1000)
                return 0;

            var products = _productRepository.GetAllProducts();

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

        public decimal CalculateTotal(Order order)
        {
            order.SubTotal = CalculateSubTotal(order);

            // Sync operations execute sequentially.
            order.Discount = GetDiscount(order);
            order.ShippingCost = CalculateShippingCost(order);

            order.TotalAmount = Math.Round(
                order.SubTotal - order.Discount + order.ShippingCost,
                2,
                MidpointRounding.AwayFromZero);

            return order.TotalAmount;
        }
    }
}
