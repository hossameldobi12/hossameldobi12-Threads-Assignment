
using FileStorageDesign.Repositories;

namespace FileStorageDesign.Shipping;

public class NileCourier : ShippingCarrierBase
{
    public NileCourier(
        ProductRepository productRepository,
        IEnumerable<int>? failForOrderIds = null)
        : base(
            "NileCourier",
            productRepository,
            800,
            1200,
            1.10m,
            failForOrderIds)
    {
    }
}
