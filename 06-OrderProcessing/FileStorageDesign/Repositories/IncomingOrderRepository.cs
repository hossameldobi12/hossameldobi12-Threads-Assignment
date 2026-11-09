using FileStorageDesign.Constants;
using FileStorageDesign.logging;
using FileStorageDesign.Models;
using OrderProcessingSystem.Storage;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Nodes;

namespace FileStorageDesign.Repositories
{
    public class IncomingOrderRepository
    {
        private readonly JsonFileStore _jsonFileStore;

        public IncomingOrderRepository(JsonFileStore jsonFileStore)
        {
            _jsonFileStore = jsonFileStore;
        }

        public async Task<List<Order>> GetAllAsync(CancellationToken cancellationToken)
        {
            await Log.OpenConnectionAsync(nameof(IncomingOrderRepository), cancellationToken);
            await Log.ExecutingAsync(nameof(IncomingOrderRepository), cancellationToken);

            var orders = await _jsonFileStore.ReadAsync<Order>(
                DataFiles.IncomingOrders,
                cancellationToken);

            if (orders.Any())
                Log.Success(nameof(IncomingOrderRepository), $"Retrieved {orders.Count} incoming orders.");
            else
                Log.Info(nameof(IncomingOrderRepository), "No incoming orders found.");

            return orders;
        }
    }
}
