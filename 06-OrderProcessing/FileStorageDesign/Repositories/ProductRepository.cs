using FileStorageDesign.Constants;
using FileStorageDesign.logging;
using FileStorageDesign.Models;
using OrderProcessingSystem.Storage;

namespace FileStorageDesign.Repositories
{
    public class ProductRepository
    {
        private readonly JsonFileStore _jsonFileStore;
        public ProductRepository(JsonFileStore jsonFileStore)
        {
            _jsonFileStore = jsonFileStore;
        }
        public async Task<Product?> GetProductByIdAsync(int productId, CancellationToken cancellationToken)
        {
            await Log.OpenConnectionAsync(nameof(ProductRepository),cancellationToken);

            await Log.ExecutingAsync(nameof(ProductRepository),cancellationToken);

            var products = await _jsonFileStore.ReadAsync<Product>(DataFiles.Products, cancellationToken);
            var product = products.FirstOrDefault(p => p.Id == productId);
            if (product != null)
            {
                Log.Success(nameof(ProductRepository), $"Product found: {product.Name} (ID: {product.Id})");
            }
            else
            {
                Log.Info(nameof(ProductRepository), $"Product with ID: {productId} not found.");
            }
            return product;
        }
        public  async Task<List<Product>> GetAllProductsAsync(CancellationToken cancellationToken)
        {
            await Log.OpenConnectionAsync(nameof(ProductRepository), cancellationToken);
            await Log.ExecutingAsync(nameof(ProductRepository), cancellationToken);
            var products = await _jsonFileStore.ReadAsync<Product>(DataFiles.Products, cancellationToken);
            if(products.Any())
            {
                Log.Success(nameof(ProductRepository), $"Retrieved {products.Count} products.");
            }
            else
            {
                Log.Info(nameof(ProductRepository), "No products found.");
            }
            return products;
        }



        public async Task<bool> ReserveStockAsync(int productId, int quantity, CancellationToken ct)
        {
            if (quantity <= 0)
                return false;

            bool reserved = await UpdateStockAsync(products =>
            {
                var product = products.FirstOrDefault(p => p.Id == productId);

                if (product == null || product.StockQuantity < quantity)
                    return false;

                product.StockQuantity -= quantity;
                return true;

            }, ct);

            if (reserved)
                Log.Success(nameof(ProductRepository), $"Reserved {quantity} units of product {productId}.");
            else
                Log.Info(nameof(ProductRepository), $"Could not reserve {quantity} units of product {productId}.");

            return reserved;
        }

        public async Task<bool> ReleaseStockAsync(int productId, int quantity, CancellationToken ct)
        {
            if (quantity <= 0)
                return false;

            bool released = await UpdateStockAsync(products =>
            {
                var product = products.FirstOrDefault(p => p.Id == productId);

                if (product == null)
                    return false;

                product.StockQuantity += quantity;
                return true;

            }, ct);

            if (released)
                Log.Success(nameof(ProductRepository), $"Released {quantity} units of product {productId}.");
            else
                Log.Info(nameof(ProductRepository), $"Could not release {quantity} units of product {productId}.");

            return released;
        }

        

        private async Task<bool> UpdateStockAsync(Func<List<Product>, bool> stockAction, CancellationToken ct)
        {
            await Log.OpenConnectionAsync(nameof(ProductRepository), ct);
            await Log.BeginTransactionAsync(nameof(ProductRepository), ct);
            await Log.SimulateUpdateAsync(nameof(ProductRepository), ct);

            bool updated = false;

            await _jsonFileStore.UpdateAsync<Product>(
                DataFiles.Products,
                products =>
                {
                    updated = stockAction(products);
                },
                ct);

            await  Log.CommitAsync(nameof(ProductRepository),ct);

            return updated;
        }
        public async Task<bool> ReserveStockUnsafeAsync(
         int productId,
         int qty,
         CancellationToken ct)
        {
            if (qty <= 0)
                return false;

            await Log.OpenConnectionAsync(nameof(ProductRepository), ct);

            var products = await _jsonFileStore.ReadAsync<Product>(
                DataFiles.Products, ct);

            var product = products.FirstOrDefault(p => p.Id == productId);

            if (product == null || product.StockQuantity < qty)
                return false;

            await Log.BeginTransactionAsync(nameof(ProductRepository), ct);
            await Log.SimulateUpdateAsync(nameof(ProductRepository), ct);

            product.StockQuantity -= qty;

            await _jsonFileStore.WriteAsync(DataFiles.Products, products, ct);

            await Log.CommitAsync(nameof(ProductRepository), ct);

            return true;
        }

    }
}
