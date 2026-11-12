using FileStorageDesign.logging;
using FileStorageDesign.Models;
using FileStorageDesign.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileStorageDesign.Services
{
    public class InventoryService
    {
        private readonly ProductRepository _productRepository;

        public InventoryService(ProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<bool> ReserveStockAsync(Order order, CancellationToken cancellationToken)
        {
            List<OrderItem> reservedItems = new List<OrderItem>();

            try
            {
                foreach (var item in order.Items)
                {
                    bool reserved = await _productRepository.ReserveStockAsync(item.ProductId, item.Quantity, cancellationToken);
                    if (!reserved)
                    {
                        foreach (var reservedItem in reservedItems)
                        {
                            await _productRepository.ReleaseStockAsync(reservedItem.ProductId, reservedItem.Quantity, CancellationToken.None);
                        }
                        Log.Info(nameof(InventoryService), $"Failed to reserve stock for Product ID: {item.ProductId}. Rolling back reserved stock.");
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
                    await _productRepository.ReleaseStockAsync(item.ProductId, item.Quantity, CancellationToken.None);
                }
                Log.Error(nameof(InventoryService), $"Error reserving stock: {ex.Message}");
                throw;
            }
        }

       public async Task ReleaseStockAsync(Order order, CancellationToken cancellationToken)
        {
            foreach (var item in order.Items)
            {
                await _productRepository.ReleaseStockAsync(item.ProductId, item.Quantity, cancellationToken);
            }
        }
    }
}
