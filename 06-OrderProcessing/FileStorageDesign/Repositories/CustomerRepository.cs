using FileStorageDesign.Constants;
using FileStorageDesign.logging;
using FileStorageDesign.Models;
using OrderProcessingSystem.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileStorageDesign.Repositories
{
    public class CustomerRepository
    {
        private readonly JsonFileStore _jsonFileStore;
        public CustomerRepository(JsonFileStore jsonFileStore)
        {
            _jsonFileStore = jsonFileStore;
        }

        public  async Task<Customer?> GetCunstomerByIdAsync(int customerId, CancellationToken cancellationToken)
        {
            Log.Info(nameof(CustomerRepository), "Opening connection...");
            await Task.Delay(200, cancellationToken);

            Log.Info(nameof(CustomerRepository), "Executing query...");
            await Task.Delay(Random.Shared.Next(300, 601), cancellationToken); 
    
            var customers = await _jsonFileStore.ReadAsync<Customer>(DataFiles.Customers, cancellationToken);

            var customer = customers.FirstOrDefault(c => c.Id == customerId);
            if (customer != null)
            {
                Log.Success(nameof(CustomerRepository), $"Customer found: {customer.Name} (ID: {customer.Id})");
            }
            else
            {
                Log.Info(nameof(CustomerRepository), $"Customer with ID: {customerId} not found.");
            }
            return customer;
        }
        public Customer? GetCustomerById(int customerId)
        {
            Log.Info(nameof(CustomerRepository), "Opening connection...");
            Thread.Sleep(200);

            Log.Info(nameof(CustomerRepository), "Executing query...");
            Thread.Sleep(Random.Shared.Next(300, 601));

            var customers = _jsonFileStore.Read<Customer>(DataFiles.Customers);

            var customer = customers.FirstOrDefault(c => c.Id == customerId);

            if (customer != null)
            {
                Log.Success(nameof(CustomerRepository),
                    $"Customer found: {customer.Name} (ID: {customer.Id})");
            }
            else
            {
                Log.Info(nameof(CustomerRepository),
                    $"Customer with ID: {customerId} not found.");
            }

            return customer;
        }
    }
}
