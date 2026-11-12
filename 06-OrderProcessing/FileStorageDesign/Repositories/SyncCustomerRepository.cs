using FileStorageDesign.Constants;
using FileStorageDesign.logging;
using FileStorageDesign.Models;
using OrderProcessingSystem.Storage;

namespace FileStorageDesign.Repositories
{
    public class SyncCustomerRepository
    {
        private readonly JsonFileStore _jsonFileStore;

        public SyncCustomerRepository(JsonFileStore jsonFileStore)
        {
            _jsonFileStore = jsonFileStore;
        }

        public Customer? GetCustomerById(int customerId)
        {
            Log.Info(nameof(SyncCustomerRepository), "Opening connection...");
            Thread.Sleep(200);

            Log.Info(nameof(SyncCustomerRepository), "Executing query...");
            Thread.Sleep(Random.Shared.Next(300, 601));

            var customers = _jsonFileStore.Read<Customer>(DataFiles.Customers);

            var customer = customers.FirstOrDefault(c => c.Id == customerId);

            if (customer != null)
                Log.Success(nameof(SyncCustomerRepository), $"Customer #{customerId} found.");
            else
                Log.Info(nameof(SyncCustomerRepository), $"Customer #{customerId} not found.");

            return customer;
        }
    }
}