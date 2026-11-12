using FileStorageDesign.Models;

namespace FileStorageDesign.Services
{
    public class OrderPricerService
    {
        private readonly PricingService _pricingService;

        public OrderPricerService(PricingService pricingService)
        {
            _pricingService = pricingService;
        }

        public async Task PriceOrderSequentialAsync(Order order, CancellationToken ct)
        {
            order.SubTotal = await _pricingService.CalculateSubTotalAsync(order, ct);

            order.Discount = await _pricingService.GetDiscountAsync(order, ct);

            order.ShippingCost = await _pricingService.CalculateShippingCostAsync(order, ct);

            order.TotalAmount = Math.Round(order.SubTotal - order.Discount + order.ShippingCost, 2, MidpointRounding.AwayFromZero);
        }
        public async Task PriceOrderAsync(Order order, CancellationToken ct)
        {
            order.SubTotal = await _pricingService.CalculateSubTotalAsync(order, ct);

            var discountTask = _pricingService.GetDiscountAsync(order, ct);
            var shippingTask = _pricingService.CalculateShippingCostAsync(order, ct);

            var result =  await Task.WhenAll(discountTask, shippingTask);

            order.Discount = result[0];
            order.ShippingCost = result[1];


            order.TotalAmount = Math.Round(order.SubTotal - order.Discount + order.ShippingCost, 2, MidpointRounding.AwayFromZero);
        }
    }
}