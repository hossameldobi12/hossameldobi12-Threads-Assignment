
using FileStorageDesign.logging;
using FileStorageDesign.Models;
using FileStorageDesign.Repositories;

namespace FileStorageDesign.Services
{
    public class SyncInventoryService
    {
        private readonly ProductRepository _productRepository;

        public SyncInventoryService(ProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public bool ReserveStock(Order order)
        {
            List<OrderItem> reservedItems = new List<OrderItem>();

            try
            {
                foreach (var item in order.Items)
                {
                    bool reserved = _productRepository.ReserveStock(
                        item.ProductId,
                        item.Quantity);

                    if (!reserved)
                    {
                        foreach (var reservedItem in reservedItems)
                        {
                            _productRepository.ReleaseStock(
                                reservedItem.ProductId,
                                reservedItem.Quantity);
                        }

                        Log.Info(
                            nameof(SyncInventoryService),
                            $"Failed to reserve stock for Product ID: {item.ProductId}. Rolling back reserved stock.");

                        return false;
                    }

                    reservedItems.Add(item);
                }

                return true;
            }
            catch (Exception ex)
            {
                foreach (var item in reservedItems)
                {
                    _productRepository.ReleaseStock(
                        item.ProductId,
                        item.Quantity);
                }

                Log.Error(
                    nameof(SyncInventoryService),
                    $"Error reserving stock: {ex.Message}");

                throw;
            }
        }

        public void ReleaseStock(Order order)
        {
            foreach (var item in order.Items)
            {
                _productRepository.ReleaseStock(
                    item.ProductId,
                    item.Quantity);
            }
        }
    }
}
