
using FileStorageDesign.Constants;
using FileStorageDesign.Interfaces;
using FileStorageDesign.logging;
using FileStorageDesign.Models;
using OrderProcessingSystem.Storage;

namespace FileStorageDesign.Services
{
    public class PaymentGateway : IPaymentGateway
    {
        private readonly JsonFileStore _jsonFileStore;
        private readonly HashSet<int> _declineOrderIds;
        private readonly HashSet<int> _timeoutOrderIds;

        public PaymentGateway(
            JsonFileStore jsonFileStore,
            IEnumerable<int> declineOrderIds,
            IEnumerable<int> timeoutOrderIds)
        {
            _jsonFileStore = jsonFileStore;
            _declineOrderIds = new HashSet<int>(declineOrderIds);
            _timeoutOrderIds = new HashSet<int>(timeoutOrderIds);
        }

        public async Task<PaymentResult> ProcessPaymentAsync(
            int orderId,
            decimal amount,
            CancellationToken ct)
        {
            // Authorize
            await Task.Delay(800, ct);

            // Fraud Check
            await Task.Delay(500, ct);

            if (_declineOrderIds.Contains(orderId))
            {
                var declinedPayment = new PaymentTransaction
                {
                    OrderId = orderId,
                    Amount = amount,
                    Status = PaymentStatus.Declined,
                    TransactionId = null,
                    CreatedAt = DateTime.UtcNow
                };

                await _jsonFileStore.UpdateAsync<PaymentTransaction>(
                    DataFiles.Payments,
                    payments => payments.Add(declinedPayment),
                    ct);

                return new PaymentResult
                {
                    Success = false,
                    Status = PaymentStatus.Declined,
                    Message = "Card declined by issuer"
                };
            }

            // Capture
            int captureDelay = _timeoutOrderIds.Contains(orderId)
                ? 5000
                : 700;

            await Task.Delay(captureDelay, ct);

            string transactionId =
                "TXN-" + Guid.NewGuid().ToString("N")[..12].ToUpper();

            var payment = new PaymentTransaction
            {
                OrderId = orderId,
                Amount = amount,
                Status = PaymentStatus.Captured,
                TransactionId = transactionId,
                CreatedAt = DateTime.UtcNow
            };

            await _jsonFileStore.UpdateAsync<PaymentTransaction>(
                DataFiles.Payments,
                payments => payments.Add(payment),
                ct);

            return new PaymentResult
            {
                Success = true,
                TransactionId = transactionId,
                Status = PaymentStatus.Captured,
                Message = "Payment captured successfully"
            };
        }

        public async Task RefundAsync(
            string transactionId,
            CancellationToken ct)
        {
            await Task.Delay(500, ct);

            await _jsonFileStore.UpdateAsync<PaymentTransaction>(
                DataFiles.Payments,
                payments =>
                {
                    var payment = payments.FirstOrDefault(
                        p => p.TransactionId == transactionId);

                    if (payment == null)
                        throw new InvalidOperationException(
                            $"Payment transaction '{transactionId}' not found.");

                    payment.Status = PaymentStatus.Refunded;
                },
                ct);
        }
        public async Task<List<PaymentTransaction>> GetAllPaymentsAsync(
    CancellationToken ct)
        {
            await Log.OpenConnectionAsync(nameof(PaymentGateway), ct);
            await Log.ExecutingAsync(nameof(PaymentGateway), ct);

            return await _jsonFileStore.ReadAsync<PaymentTransaction>(
                DataFiles.Payments, ct);
        }

    }
}
