using FileStorageDesign.Models;
using FileStorageDesign.Repositories;
using FileStorageDesign.Shipping;

namespace FileStorageDesign.Services;

public class CairoExpress : ShippingCarrierBase
{
    public CairoExpress(
        ProductRepository productRepository,
        IEnumerable<int>? failForOrderIds = null)
        : base(
            "CairoExpress",
            productRepository,
            300,
            2000,
            0.95m,
            failForOrderIds)
    {
    }

    protected override bool ShouldFail(Order order)
    {
        return order.Id % 2 == 0 || base.ShouldFail(order);
    }
}
