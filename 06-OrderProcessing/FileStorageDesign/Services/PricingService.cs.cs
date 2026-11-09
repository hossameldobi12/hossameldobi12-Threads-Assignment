using FileStorageDesign.Exceptions;
using FileStorageDesign.logging;
using FileStorageDesign.Models;
using FileStorageDesign.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileStorageDesign.Services
{
    public class PricingService
    {
        private readonly ProductRepository _productRepository;
        private readonly CouponRepository _couponRepository;
        private readonly CustomerRepository _customerRepository;

        private readonly HashSet<int> _failShippingForOrderIds;

        public PricingService(ProductRepository productRepository, CouponRepository couponRepository, CustomerRepository customerRepository, IEnumerable<int> failShippingForOrderIds)
        {
            _productRepository = productRepository;
            _couponRepository = couponRepository;
            _customerRepository = customerRepository;
            _failShippingForOrderIds = new HashSet<int>(failShippingForOrderIds);
        }


        public async Task<decimal> CalculateSubTotalAsync(Order order, CancellationToken ct)
        {
            decimal subTotal = 0;
            var products = await _productRepository.GetAllProductsAsync(ct);
            foreach (var item in order.Items)
            {
                var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                if (product == null)
                {
                    throw new Exception($"Product with ID {item.ProductId} not found.");
                }
                item.UnitPrice = product.Price;
                subTotal += item.UnitPrice * item.Quantity;
            }
            subTotal = Math.Round(subTotal, 2, MidpointRounding.AwayFromZero);
            return subTotal;
        }
        public async Task<decimal> GetDiscountAsync(Order order, CancellationToken ct)
        {
            decimal discount = 0;
            var customer = await _customerRepository.GetCunstomerByIdAsync(order.CustomerId, ct);
            if (customer == null)
            {
                throw new Exception($"Customer with ID {order.CustomerId} not found.");

            }
            discount = customer.LoyaltyTier switch
            {
                LoyaltyTier.Gold => 10,
                LoyaltyTier.Silver => 5,
                LoyaltyTier.Bronze => 0,
                _ => 0
            };

            if (!string.IsNullOrEmpty(order.CouponCode))
            {
                var coupon = await _couponRepository.GetCouponByCodeAsync(order.CouponCode, ct);
                if (coupon != null && coupon.ExpiryDate >= DateTime.Today)
                {
                    discount += coupon.DiscountPercent;
                }
                else
                {
                    throw new Exception($"Coupon with code {order.CouponCode} is either invalid or expired.");

                }
            }
            discount = Math.Min(discount, 30);
            discount = Math.Round((order.SubTotal * discount) / 100, 2, MidpointRounding.AwayFromZero);
            return discount;




        }
        public async Task<decimal> CalculateShippingCostAsync(Order order, CancellationToken ct)
        {
            Log.Info("Shipping API", $"Starting handshake for Order #{order.Id}");

            await Task.Delay(200, ct);

            if (_failShippingForOrderIds.Contains(order.Id))
            {
                Log.Error("Shipping API", $"Shipping provider unavailable for Order #{order.Id}");
                throw new ShippingProviderUnavailableException(
                    "Shipping provider unavailable");
            }
            decimal shippingCost = 0;
            if (order.SubTotal >= 1000)
                return 0;

            var products = await _productRepository.GetAllProductsAsync(ct);

            decimal totalQuantity = 0;
            decimal totalWeight = 0;

            foreach (var item in order.Items)
            {
                var product = products.FirstOrDefault(p => p.Id == item.ProductId);

                if (product == null)
                {
                    throw new Exception($"Product with ID {item.ProductId} not found.");
                }

                totalQuantity += item.Quantity;
                totalWeight += product.WeightKg * item.Quantity;
            }

            shippingCost = 5 + (1.5m * totalQuantity) + (2m * totalWeight);

            shippingCost = Math.Round(shippingCost, 2, MidpointRounding.AwayFromZero);
            return shippingCost;
        }

    }
}
