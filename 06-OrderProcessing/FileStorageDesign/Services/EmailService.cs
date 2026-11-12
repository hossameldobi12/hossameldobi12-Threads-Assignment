
using System.Collections.Concurrent;
using FileStorageDesign.Constants;
using FileStorageDesign.Models;
using FileStorageDesign.logging;
using OrderProcessingSystem.Storage;
using FileStorageDesign.Exceptions;

namespace FileStorageDesign.Services
{
    public class EmailService : IEmailService
    {
        private readonly JsonFileStore _jsonFileStore;
        private readonly Dictionary<int, int> _failAttempts;
        private readonly ConcurrentDictionary<int, int> _attemptCounts = new();

        public EmailService(
            JsonFileStore jsonFileStore,
            Dictionary<int, int> failAttempts)
        {
            _jsonFileStore = jsonFileStore;
            _failAttempts = failAttempts;
        }

        public async Task SendConfirmationAsync(
            Order order,
            Customer customer,
            CancellationToken cancellationToken)
        {
            int attempt = _attemptCounts.AddOrUpdate(
                order.Id,
                1,
                (_, current) => current + 1);

            Log.Info(nameof(EmailService),
                $"Sending email for Order #{order.Id}, Attempt #{attempt}");

            Log.Info(nameof(EmailService),
                "Rendering email template...");

            await Task.Delay(200, cancellationToken);

            Log.Info(nameof(EmailService),
                "Connecting to SMTP server...");

            await Task.Delay(300, cancellationToken);

            Log.Info(nameof(EmailService),
                $"Sending confirmation email to {customer.Email}");

            await Task.Delay(500, cancellationToken);

            if (_failAttempts.TryGetValue(order.Id, out int failures)
                && attempt <= failures)
            {
                Log.Error(nameof(EmailService),
                    $"Email attempt #{attempt} failed for Order #{order.Id}");

                throw new SmtpUnavailableException(
                    "SMTP server is unavailable");
            }

            var email = new EmailRecord
            {
                OrderId = order.Id,
                CustomerId = customer.Id,
                EmailAddress = customer.Email,
                Attempts = attempt,
                SentAt = DateTime.UtcNow
            };

            await _jsonFileStore.UpdateAsync<EmailRecord>(
                DataFiles.Emails,
                emails => emails.Add(email),
                cancellationToken);

            Log.Success(nameof(EmailService),
                $"Confirmation email sent for Order #{order.Id}");
        }
        public async Task<List<EmailRecord>> GetAllEmailsAsync(
    CancellationToken ct)
        {
            await Log.OpenConnectionAsync(nameof(EmailService), ct);
            await Log.ExecutingAsync(nameof(EmailService), ct);

            return await _jsonFileStore.ReadAsync<EmailRecord>(
                DataFiles.Emails, ct);
        }
    }
}
