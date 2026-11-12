using FileStorageDesign.Constants;
using FileStorageDesign.logging;
using FileStorageDesign.Models;
using OrderProcessingSystem.Storage;


namespace FileStorageDesign.Repositories
{
    public class CouponRepository
    {
        private readonly JsonFileStore _jsonFileStore;
        public CouponRepository(JsonFileStore jsonFileStore)
        {
            _jsonFileStore = jsonFileStore;
        }
        public async Task<Coupon?> GetCouponByCodeAsync(string couponCode, CancellationToken cancellationToken)
        {
            await Log.OpenConnectionAsync(nameof(CouponRepository), cancellationToken);
            await Log.ExecutingAsync(nameof(CouponRepository), cancellationToken);
            var coupons = await _jsonFileStore.ReadAsync<Coupon>(DataFiles.Coupons, cancellationToken);
            var coupon = coupons.FirstOrDefault(c => c.Code.Equals(couponCode, StringComparison.OrdinalIgnoreCase));
            if (coupon != null)
            {
                Log.Success(nameof(CouponRepository), $"Coupon found: {coupon.Code} ");
            }
            else
            {
                Log.Info(nameof(CouponRepository), $"Coupon with code: {couponCode} not found.");
            }
            return coupon;
        }
        public Coupon? GetCouponByCode(string couponCode)
        {
            Log.Info(nameof(CouponRepository), "Opening connection...");
            Thread.Sleep(200);

            Log.Info(nameof(CouponRepository), "Executing query...");
            Thread.Sleep(Random.Shared.Next(300, 601));

            var coupons = _jsonFileStore.Read<Coupon>(DataFiles.Coupons);

            var coupon = coupons.FirstOrDefault(
                c => c.Code.Equals(couponCode, StringComparison.OrdinalIgnoreCase));

            if (coupon != null)
            {
                Log.Success(nameof(CouponRepository), $"Coupon found: {coupon.Code}");
            }
            else
            {
                Log.Info(nameof(CouponRepository), $"Coupon with code: {couponCode} not found.");
            }

            return coupon;
        }
    }
}
