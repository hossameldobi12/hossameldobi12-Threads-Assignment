using FileStorageDesign.Constants;
using FileStorageDesign.logging;
using FileStorageDesign.Models;
using OrderProcessingSystem.Storage;

namespace FileStorageDesign.Repositories
{
    public class SyncProductRepository
    {
        private readonly JsonFileStore _jsonFileStore;

        public SyncProductRepository(JsonFileStore jsonFileStore)
        {
            _jsonFileStore = jsonFileStore;
        }

        public Product? GetProductById(int productId)
        {
            Log.Info(nameof(SyncProductRepository), "Opening connection...");
            Thread.Sleep(200);

            Log.Info(nameof(SyncProductRepository), "Executing query...");
            Thread.Sleep(Random.Shared.Next(300, 601));

            var products = _jsonFileStore.Read<Product>(DataFiles.Products);

            var product = products.FirstOrDefault(p => p.Id == productId);

            if (product != null)
                Log.Success(nameof(SyncProductRepository), $"Product found: {product.Name} (ID: {product.Id})");
            else
                Log.Info(nameof(SyncProductRepository), $"Product with ID: {productId} not found.");

            return product;
        }

        public bool ReserveStock(int productId, int quantity)
        {
            if (quantity <= 0)
                return false;

            bool reserved = UpdateStock(products =>
            {
                var product = products.FirstOrDefault(p => p.Id == productId);

                if (product == null || product.StockQuantity < quantity)
                    return false;

                product.StockQuantity -= quantity;
                return true;
            });

            if (reserved)
                Log.Success(nameof(SyncProductRepository), $"Reserved {quantity} units of product {productId}.");
            else
                Log.Info(nameof(SyncProductRepository), $"Could not reserve {quantity} units of product {productId}.");

            return reserved;
        }

        public bool ReleaseStock(int productId, int quantity)
        {
            if (quantity <= 0)
                return false;

            bool released = UpdateStock(products =>
            {
                var product = products.FirstOrDefault(p => p.Id == productId);

                if (product == null)
                    return false;

                product.StockQuantity += quantity;
                return true;
            });

            if (released)
                Log.Success(nameof(SyncProductRepository), $"Released {quantity} units of product {productId}.");
            else
                Log.Info(nameof(SyncProductRepository), $"Could not release {quantity} units of product {productId}.");

            return released;
        }

        private bool UpdateStock(Func<List<Product>, bool> stockAction)
        {
            Log.Info(nameof(SyncProductRepository), "Opening connection...");
            Thread.Sleep(200);

            Log.Info(nameof(SyncProductRepository), "Beginning transaction...");
            Thread.Sleep(100);

            Log.Info(nameof(SyncProductRepository), "Updating product stock...");
            Thread.Sleep(400);

            bool updated = false;

            _jsonFileStore.Update<Product>(
                DataFiles.Products,
                products =>
                {
                    updated = stockAction(products);
                });

            Log.Info(nameof(SyncProductRepository), "Committing transaction...");
            Thread.Sleep(200);

            return updated;
        }
    }
}