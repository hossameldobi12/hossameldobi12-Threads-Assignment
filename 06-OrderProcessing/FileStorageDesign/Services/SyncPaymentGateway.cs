
using FileStorageDesign.Constants;
using FileStorageDesign.Models;
using OrderProcessingSystem.Storage;

namespace FileStorageDesign.Services
{
    public class SyncPaymentGateway
    {
        private readonly JsonFileStore _jsonFileStore;
        private readonly HashSet<int> _declineOrderIds;
        private readonly HashSet<int> _timeoutOrderIds;

        public SyncPaymentGateway(
            JsonFileStore jsonFileStore,
            IEnumerable<int> declineOrderIds,
            IEnumerable<int> timeoutOrderIds)
        {
            _jsonFileStore = jsonFileStore;
            _declineOrderIds = new HashSet<int>(declineOrderIds);
            _timeoutOrderIds = new HashSet<int>(timeoutOrderIds);
        }

        public PaymentResult ProcessPayment(
            int orderId,
            decimal amount)
        {
            // Authorize
            Thread.Sleep(800);

            // Fraud Check
            Thread.Sleep(500);

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

                _jsonFileStore.Update<PaymentTransaction>(
                    DataFiles.Payments,
                    payments => payments.Add(declinedPayment));

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

            Thread.Sleep(captureDelay);

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

            _jsonFileStore.Update<PaymentTransaction>(
                DataFiles.Payments,
                payments => payments.Add(payment));

            return new PaymentResult
            {
                Success = true,
                TransactionId = transactionId,
                Status = PaymentStatus.Captured,
                Message = "Payment captured successfully"
            };
        }

        public void Refund(string transactionId)
        {
            Thread.Sleep(500);

            _jsonFileStore.Update<PaymentTransaction>(
                DataFiles.Payments,
                payments =>
                {
                    var payment = payments.FirstOrDefault(
                        p => p.TransactionId == transactionId);

                    if (payment == null)
                        throw new InvalidOperationException(
                            $"Payment transaction '{transactionId}' not found.");

                    payment.Status = PaymentStatus.Refunded;
                });
        }
    }
}
