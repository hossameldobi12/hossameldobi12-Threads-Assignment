using FileStorageDesign.Models;
using FileStorageDesign.Repositories;

namespace FileStorageDesign.Services
{
    public class SyncOrderValidator
    {
        private readonly SyncCouponRepository _couponRepository;
        private readonly SyncCustomerRepository _customerRepository;
        private readonly SyncProductRepository _productRepository;

        public SyncOrderValidator(
            SyncCouponRepository couponRepository,
            SyncCustomerRepository customerRepository,
            SyncProductRepository productRepository)
        {
            _couponRepository = couponRepository;
            _customerRepository = customerRepository;
            _productRepository = productRepository;
        }

        public void ValidateOrder(Order order)
        {
            var failureReason = ValidateCustomer(order);

            if (failureReason != null)
            {
                RejectOrder(order, failureReason);
                return;
            }

            var products = new List<Product>();

            failureReason = ValidateProductsExist(order, products);

            if (failureReason != null)
            {
                RejectOrder(order, failureReason);
                return;
            }

            failureReason = ValidateStock(order, products);

            if (failureReason != null)
            {
                RejectOrder(order, failureReason);
                return;
            }

            failureReason = ValidateCoupon(order);

            if (failureReason != null)
            {
                RejectOrder(order, failureReason);
                return;
            }

            order.Status = OrderStatus.Validated;
            order.FailureReason = null;
        }

        private string? ValidateCustomer(Order order)
        {
            var customer = _customerRepository.GetCustomerById(order.CustomerId);

            if (customer == null)
                return $"Customer #{order.CustomerId} not found";

            if (!customer.IsActive)
                return "Customer is inactive";

            return null;
        }

        private string? ValidateProductsExist(
            Order order,
            List<Product> products)
        {
            foreach (var item in order.Items)
            {
                var product = _productRepository.GetProductById(item.ProductId);

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

        private string? ValidateCoupon(Order order)
        {
            if (order.CouponCode == null)
                return null;

            var coupon = _couponRepository.GetCouponByCode(order.CouponCode);

            if (coupon == null)
                return $"Coupon '{order.CouponCode}' does not exist";

            if (coupon.ExpiryDate < DateTime.Today)
                return $"Coupon '{order.CouponCode}' has expired";

            return null;
        }

        private void RejectOrder(Order order, string failureReason)
        {
            order.Status = OrderStatus.Rejected;
            order.FailureReason = failureReason;
        }
    }
}