

using FileStorageDesign.Constants;
using FileStorageDesign.Exceptions;
using FileStorageDesign.logging;
using FileStorageDesign.Models;
using OrderProcessingSystem.Storage;

namespace FileStorageDesign.Repositories
{
    public class OrderRepository
    {
        private readonly JsonFileStore _jsonFileStore;
        private readonly HashSet<int> _failSaveForOrderIds;
        public OrderRepository(JsonFileStore jsonFileStore, IEnumerable<int> failSaveForOrderIds)
        {
         
            _jsonFileStore = jsonFileStore;
            _failSaveForOrderIds = new HashSet<int>(failSaveForOrderIds);
        }

        public async Task<List<Order>> GetAllOrderAsync(CancellationToken cancellationToken)
        {
            await Log.OpenConnectionAsync(nameof(OrderRepository), cancellationToken);

            await Log.ExecutingAsync(nameof(OrderRepository), cancellationToken);

            var orders = await _jsonFileStore.ReadAsync<Order>(DataFiles.Orders, cancellationToken);
            if(orders.Any())
            {
                Log.Success(nameof(OrderRepository), $"Retrieved {orders.Count} orders.");

            }
            else
            {
                Log.Info(nameof(OrderRepository), "No orders found.");
            }
            return orders;

        }
     

        public async Task<Order?> GetOrderByIdAsync(int orderId, CancellationToken cancellationToken)
        {
            await Log.OpenConnectionAsync(nameof(OrderRepository), cancellationToken);
            await Log.ExecutingAsync(nameof(OrderRepository), cancellationToken);
            var orders = await _jsonFileStore.ReadAsync<Order>(DataFiles.Orders, cancellationToken);
            var order = orders.FirstOrDefault(o => o.Id == orderId);
            if (order != null)
            {
                Log.Success(nameof(OrderRepository), $"Order found: {order.Id}");
            }
            else
            {
                Log.Info(nameof(OrderRepository), $"Order with ID: {orderId} not found.");
            }
            return order;
        }

        public async Task<bool> SaveOrderAsync(
            Order order,
            CancellationToken cancellationToken)
        {
            await Log.OpenConnectionAsync(
                nameof(OrderRepository), cancellationToken);

            await Log.BeginTransactionAsync(
                nameof(OrderRepository), cancellationToken);

            await Log.SimulateUpdateAsync(
                nameof(OrderRepository), cancellationToken);
            // Controlled failure during commit
            if (_failSaveForOrderIds.Contains(order.Id)
                && order.Status == OrderStatus.Completed)
            {
                throw new DatabaseWriteException(
                    "Connection lost during commit");
            }

            await _jsonFileStore.UpdateAsync<Order>(
                DataFiles.Orders,
                orders =>
                {
                    var index = orders.FindIndex(o => o.Id == order.Id);

                    if (index == -1)
                        orders.Add(order);
                    else
                        orders[index] = order;
                },
                cancellationToken);

          

            await Log.CommitAsync(
                nameof(OrderRepository), cancellationToken);

            Log.Success(
                nameof(OrderRepository),
                $"Order saved: {order.Id}");

            return true;
        }


        public async Task<List<Order>> GetByStatusAsync(OrderStatus orderStatus,CancellationToken cancellationToken)
        {
            await Log.OpenConnectionAsync(nameof(OrderRepository), cancellationToken);
            await Log.ExecutingAsync(nameof(OrderRepository), cancellationToken);
            var orders = await _jsonFileStore.ReadAsync<Order>(DataFiles.Orders, cancellationToken);
            var  orderSameStatus = orders.Where(o => o.Status == orderStatus).ToList();
            if (orderSameStatus.Any())
            {
                Log.Success(nameof(OrderRepository), $"Retrieved {orderSameStatus.Count} orders.");

            }
            else
            {
                Log.Info(nameof(OrderRepository), "No orders found.");
            }
            return  orderSameStatus;


        }
        public bool SaveOrder(Order order)
        {
            Log.Info(nameof(OrderRepository), "Opening connection...");
            Thread.Sleep(200);

            Log.Info(nameof(OrderRepository), "Beginning transaction...");
            Thread.Sleep(100);

            Log.Info(nameof(OrderRepository), "Updating order...");

            Thread.Sleep(400);

            // Controlled failure during commit
            if (_failSaveForOrderIds.Contains(order.Id)
                && order.Status == OrderStatus.Completed)
            {
                throw new DatabaseWriteException(
                    "Connection lost during commit");
            }

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

            Log.Info(nameof(OrderRepository), "Committing transaction...");
            Thread.Sleep(200);

            Log.Success(nameof(OrderRepository),
                $"Order saved: {order.Id}");

            return true;
        }
    }
}
