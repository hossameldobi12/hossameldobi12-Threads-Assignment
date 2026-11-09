using FileStorageDesign.Constants;
using FileStorageDesign.logging;
using FileStorageDesign.Models;
using OrderProcessingSystem.Storage;

namespace FileStorageDesign.Repositories
{
    public class SyncCouponRepository
    {
        private readonly JsonFileStore _jsonFileStore;

        public SyncCouponRepository(JsonFileStore jsonFileStore)
        {
            _jsonFileStore = jsonFileStore;
        }

        public Coupon? GetCouponByCode(string couponCode)
        {
            Log.Info(nameof(SyncCouponRepository), "Opening connection...");
            Thread.Sleep(200);

            Log.Info(nameof(SyncCouponRepository), "Executing query...");
            Thread.Sleep(Random.Shared.Next(300, 601));

            var coupons = _jsonFileStore.Read<Coupon>(DataFiles.Coupons);

            var coupon = coupons.FirstOrDefault(
                c => c.Code.Equals(couponCode, StringComparison.OrdinalIgnoreCase));

            if (coupon != null)
                Log.Success(nameof(SyncCouponRepository), $"Coupon found: {coupon.Code}");
            else
                Log.Info(nameof(SyncCouponRepository), $"Coupon '{couponCode}' not found.");

            return coupon;
        }
    }
}