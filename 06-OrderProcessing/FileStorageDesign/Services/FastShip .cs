
using FileStorageDesign.Repositories;

namespace FileStorageDesign.Shipping;

public class FastShip : ShippingCarrierBase
{
    public FastShip(
        ProductRepository productRepository,
        IEnumerable<int>? failForOrderIds = null)
        : base(
            "FastShip",
            productRepository,
            500,
            1500,
            1.00m,
            failForOrderIds)
    {
    }
}
