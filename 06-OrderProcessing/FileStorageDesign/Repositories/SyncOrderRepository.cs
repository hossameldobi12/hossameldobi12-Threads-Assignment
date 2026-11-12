using FileStorageDesign.Constants;
using FileStorageDesign.logging;
using FileStorageDesign.Models;
using OrderProcessingSystem.Storage;

namespace FileStorageDesign.Repositories
{
    public class SyncOrderRepository
    {
        private readonly JsonFileStore _jsonFileStore;

        public SyncOrderRepository(JsonFileStore jsonFileStore)
        {
            _jsonFileStore = jsonFileStore;
        }

        public List<Order> GetAllOrders()
        {
            Log.Info(nameof(SyncOrderRepository), "Opening connection...");
            Thread.Sleep(200);

            Log.Info(nameof(SyncOrderRepository), "Executing query...");
            Thread.Sleep(Random.Shared.Next(300, 601));

            var orders = _jsonFileStore.Read<Order>(DataFiles.Orders);

            if (orders.Any())
                Log.Success(nameof(SyncOrderRepository), $"Retrieved {orders.Count} orders.");
            else
                Log.Info(nameof(SyncOrderRepository), "No orders found.");

            return orders;
        }

        public Order? GetOrderById(int orderId)
        {
            Log.Info(nameof(SyncOrderRepository), "Opening connection...");
            Thread.Sleep(200);

            Log.Info(nameof(SyncOrderRepository), "Executing query...");
            Thread.Sleep(Random.Shared.Next(300, 601));

            var orders = _jsonFileStore.Read<Order>(DataFiles.Orders);

            var order = orders.FirstOrDefault(o => o.Id == orderId);

            if (order != null)
                Log.Success(nameof(SyncOrderRepository), $"Order found: {order.Id}");
            else
                Log.Info(nameof(SyncOrderRepository), $"Order with ID: {orderId} not found.");

            return order;
        }

        public bool SaveOrder(Order order)
        {
            Log.Info(nameof(SyncOrderRepository), "Opening connection...");
            Thread.Sleep(200);

            Log.Info(nameof(SyncOrderRepository), "Beginning transaction...");
            Thread.Sleep(100);

            Log.Info(nameof(SyncOrderRepository), "Updating order...");
            Thread.Sleep(400);

            _jsonFileStore.Update<Order>(
                DataFiles.Orders,
                orders =>
                {
                    var index = orders.FindIndex(o => o.Id == order.Id);

                    if (index == -1)
                        orders.Add(order);
                    else
                        orders[index] = order;
                });

            Log.Info(nameof(SyncOrderRepository), "Committing transaction...");
            Thread.Sleep(200);

            Log.Success(nameof(SyncOrderRepository), $"Order saved: {order.Id}");

            return true;
        }

        public List<Order> GetByStatus(OrderStatus orderStatus)
        {
            Log.Info(nameof(SyncOrderRepository), "Opening connection...");
            Thread.Sleep(200);

            Log.Info(nameof(SyncOrderRepository), "Executing query...");
            Thread.Sleep(Random.Shared.Next(300, 601));

            var orders = _jsonFileStore.Read<Order>(DataFiles.Orders);

            var ordersSameStatus = orders
                .Where(o => o.Status == orderStatus)
                .ToList();

            if (ordersSameStatus.Any())
                Log.Success(nameof(SyncOrderRepository), $"Retrieved {ordersSameStatus.Count} orders.");
            else
                Log.Info(nameof(SyncOrderRepository), "No orders found.");

            return ordersSameStatus;
        }
    }
}