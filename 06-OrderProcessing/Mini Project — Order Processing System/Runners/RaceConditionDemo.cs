using FileStorageDesign.Constants;
using FileStorageDesign.Models;
using FileStorageDesign.Repositories;
using OrderProcessingSystem.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mini_Project___Order_Processing_System.Runners
{
    public class RaceConditionDemo
    {
        private readonly ProductRepository _productRepository;
        private readonly JsonFileStore _jsonFileStore; 

        public RaceConditionDemo(ProductRepository productRepository,JsonFileStore jsonFileStore)
        {
            _productRepository = productRepository;
            _jsonFileStore = jsonFileStore;
        }
        private async Task SetKeyboardStockAsync(CancellationToken ct)
        {
            await _jsonFileStore.UpdateAsync<Product>(
                DataFiles.Products,
                products =>
                {
                    var keyboard = products.First(p => p.Id == 4);
                    keyboard.StockQuantity = 1;
                },
                ct);
        }
        public async Task RunAsync(CancellationToken ct)
        {
            Console.WriteLine("\n========== Race Condition Demo ==========");

            await SetKeyboardStockAsync(ct);

            var order10Task = _productRepository.ReserveStockUnsafeAsync(4, 1, ct);
            var order11Task = _productRepository.ReserveStockUnsafeAsync(4, 1, ct);

            var results = await Task.WhenAll(order10Task, order11Task);

            var products = await _jsonFileStore.ReadAsync<Product>(
                DataFiles.Products, ct);

            var keyboard = products.First(p => p.Id == 4);

            Console.WriteLine($"Order #10 reserved: {results[0]}");
            Console.WriteLine($"Order #11 reserved: {results[1]}");
            Console.WriteLine($"Keyboard stock: {keyboard.StockQuantity}");

            Console.WriteLine("=========================================");

            Console.WriteLine("\n========== Safe Reservation Demo ==========");

            await SetKeyboardStockAsync(ct);

            var safeOrder10Task = _productRepository.ReserveStockAsync(4, 1, ct);
            var safeOrder11Task = _productRepository.ReserveStockAsync(4, 1, ct);

            var safeResults = await Task.WhenAll(safeOrder10Task, safeOrder11Task);

            var safeProducts = await _jsonFileStore.ReadAsync<Product>(
                DataFiles.Products, ct);

            var safeKeyboard = safeProducts.First(p => p.Id == 4);

            Console.WriteLine($"Order #10 reserved: {safeResults[0]}");
            Console.WriteLine($"Order #11 reserved: {safeResults[1]}");
            Console.WriteLine($"Keyboard stock: {safeKeyboard.StockQuantity}");

            Console.WriteLine("===========================================");
        }



    }
}
