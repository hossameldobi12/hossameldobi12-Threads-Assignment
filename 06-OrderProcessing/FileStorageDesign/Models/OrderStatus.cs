namespace FileStorageDesign.Models;

public enum OrderStatus
{
    Pending,
    Validated,
    Rejected,
    PaymentFailed,
    Completed,
    Failed
}