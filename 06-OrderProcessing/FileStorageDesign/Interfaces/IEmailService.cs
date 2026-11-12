using FileStorageDesign.Models;

public interface IEmailService
{
    Task SendConfirmationAsync(
        Order order,
        Customer customer,
        CancellationToken cancellationToken);
    public Task<List<EmailRecord>> GetAllEmailsAsync(CancellationToken ct);
}