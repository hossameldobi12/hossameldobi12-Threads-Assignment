using FileStorageDesign.Models;

namespace FileStorageDesign.Interfaces
{
    public interface IPaymentGateway
    {
        Task<PaymentResult> ProcessPaymentAsync( int orderId,decimal amount,CancellationToken ct);

        Task RefundAsync( string transactionId, CancellationToken ct);
        public  Task<List<PaymentTransaction>> GetAllPaymentsAsync(CancellationToken ct);
    }
}
