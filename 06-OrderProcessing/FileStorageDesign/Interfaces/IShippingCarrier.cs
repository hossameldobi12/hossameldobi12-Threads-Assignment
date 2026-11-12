using FileStorageDesign.Models;

namespace FileStorageDesign.Interfaces;

public interface IShippingCarrier
{
    string Name { get; }
    Task<decimal> GetQuoteAsync(
        Order order,
        CancellationToken ct);
}
