
using System.Collections.Concurrent;
using FileStorageDesign.Constants;
using FileStorageDesign.Exceptions;
using FileStorageDesign.logging;
using FileStorageDesign.Models;
using OrderProcessingSystem.Storage;

namespace FileStorageDesign.Services
{
    public class SyncEmailService
    {
        private readonly JsonFileStore _jsonFileStore;
        private readonly Dictionary<int, int> _failAttempts;
        private readonly ConcurrentDictionary<int, int> _attemptCounts = new();

        public SyncEmailService(
            JsonFileStore jsonFileStore,
            Dictionary<int, int> failAttempts)
        {
            _jsonFileStore = jsonFileStore;
            _failAttempts = failAttempts;
        }

        public void SendConfirmation(Order order, Customer customer)
        {
            int attempt = _attemptCounts.AddOrUpdate(
                order.Id,
                1,
                (_, current) => current + 1);

            Log.Info(nameof(SyncEmailService),
                $"Sending email for Order #{order.Id}, Attempt #{attempt}");

            Log.Info(nameof(SyncEmailService),
                "Rendering email template...");

            Thread.Sleep(200);

            Log.Info(nameof(SyncEmailService),
                "Connecting to SMTP server...");

            Thread.Sleep(300);

            Log.Info(nameof(SyncEmailService),
                $"Sending confirmation email to {customer.Email}");

            Thread.Sleep(500);

            if (_failAttempts.TryGetValue(order.Id, out int failures)
                && attempt <= failures)
            {
                Log.Error(nameof(SyncEmailService),
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

            _jsonFileStore.Update<EmailRecord>(
                DataFiles.Emails,
                emails => emails.Add(email));

            Log.Success(nameof(SyncEmailService),
                $"Confirmation email sent for Order #{order.Id}");
        }
    }
}
