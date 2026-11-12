
using FileStorageDesign.Exceptions;
using FileStorageDesign.Interfaces;
using FileStorageDesign.logging;
using FileStorageDesign.Models;
using FileStorageDesign.Repositories;

namespace FileStorageDesign.Shipping;

public abstract class ShippingCarrierBase : IShippingCarrier
{
    private readonly ProductRepository _productRepository;
    private readonly HashSet<int> _failForOrderIds;

    private readonly int _minDelay;
    private readonly int _maxDelay;
    private readonly decimal _priceFactor;

    protected string CarrierName { get; }

    public string Name => CarrierName;

    protected ShippingCarrierBase(
        string carrierName,
        ProductRepository productRepository,
        int minDelay,
        int maxDelay,
        decimal priceFactor,
        IEnumerable<int>? failForOrderIds = null)
    {
        CarrierName = carrierName;
        _productRepository = productRepository;
        _minDelay = minDelay;
        _maxDelay = maxDelay;
        _priceFactor = priceFactor;

        _failForOrderIds = failForOrderIds?.ToHashSet()
            ?? new HashSet<int>();
    }

    protected virtual bool ShouldFail(Order order)
    {
        return false;
    }

    public async Task<decimal> GetQuoteAsync(
        Order order,
        CancellationToken ct)
    {
        try
        {
            // 1. Handshake
            Log.Info(
                CarrierName,
                $"Starting handshake for Order #{order.Id}");

            await Task.Delay(150, ct);

            // 2. Controlled test failure
            if (_failForOrderIds.Contains(order.Id))
            {
                throw new ShippingProviderUnavailableException(
                    $"{CarrierName} unavailable for Order #{order.Id}");
            }

            // 3. Random response delay
            await Task.Delay(
                Random.Shared.Next(_minDelay, _maxDelay + 1),
                ct);

            // 4. Built-in carrier failure
            if (ShouldFail(order))
            {
                throw new ShippingProviderUnavailableException(
                    $"{CarrierName} unavailable for Order #{order.Id}");
            }

            // 5. Calculate shipping cost
            decimal shippingCost = 0;

            if (order.SubTotal < 1000)
            {
                var products =
                    await _productRepository.GetAllProductsAsync(ct);

                decimal totalQuantity = 0;
                decimal totalWeight = 0;

                foreach (var item in order.Items)
                {
                    var product = products.FirstOrDefault(
                        p => p.Id == item.ProductId);

                    if (product == null)
                    {
                        throw new Exception(
                            $"Product with ID {item.ProductId} not found.");
                    }

                    totalQuantity += item.Quantity;

                    totalWeight +=
                        product.WeightKg * item.Quantity;
                }

                shippingCost =
                    5 + (1.5m * totalQuantity) + (2m * totalWeight);
            }

            // 6. Apply carrier price factor
            var finalPrice = Math.Round(
                shippingCost * _priceFactor,
                2,
                MidpointRounding.AwayFromZero);

            Log.Success(
                CarrierName,
                $"Quote for Order #{order.Id}: {finalPrice}");

            return finalPrice;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            Log.Info(CarrierName, "cancelled");
            throw;
        }
    }
}
