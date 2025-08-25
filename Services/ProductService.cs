using ABC_Retail.Models;
using ABC_Retail.Models.DTOs;
using ABC_Retail.Services.Queues;
using Azure;
using Azure.Data.Tables;

namespace ABC_Retail.Services
{
    public class ProductService
    {
        private readonly TableClient _table;
        private readonly ProductQueueService _queue;
        private readonly ILogger<ProductService> _logger;


        public ProductService(TableServiceClient serviceClient, ProductQueueService queue, ILogger<ProductService> logger)
        {
            _table = serviceClient.GetTableClient("Products");
            _table.CreateIfNotExists(); // Safe init
            _queue = queue;
            _logger = logger;
        }

        public async Task AddProductAsync(Product product)
        {
            await _table.AddEntityAsync(product);

            var message = new ProductChangeMessageDto
            {
                RowKey = product.RowKey,
                Timestamp = DateTime.UtcNow,
                ChangeType = "Create",
                Name = product.Name
            };

            await _queue.EnqueueProductChangeAsync(message);
        }


        //READ: All products
        public async Task<List<Product>> GetProductsAsync()
        {
            var items = new List<Product>();
            await foreach (Product p in _table.QueryAsync<Product>())
                items.Add(p);
            return items;
        }

        //READ: Single product by RowKey
        public async Task<Product?> GetProductAsync(string rowKey)
        {
            try
            {
                var response = await _table.GetEntityAsync<Product>("Retail", rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null; // Not found
            }
        }
        public async Task UpdateProductAsync(Product updatedProduct)
        {
            _logger.LogInformation("🛠️ UpdateProductAsync triggered for {ProductName}", updatedProduct.Name);


            // 1️⃣ Fetch previous state
            var previousProduct = await GetProductAsync(updatedProduct.RowKey);

            // 2️⃣ Update in Table Storage
            await _table.UpdateEntityAsync(updatedProduct, updatedProduct.ETag);

            // 3️⃣ Create and enqueue update message
            var message = new ProductChangeMessageDto
            {
                RowKey = updatedProduct.RowKey,
                ChangeType = "Update",
                Timestamp = DateTime.UtcNow,
                Name = updatedProduct.Name,
                Price = updatedProduct.Price,
                StockQty = updatedProduct.StockQty,
                PreviousPrice = previousProduct?.Price ?? 0,  // fallback to 0 if null
                PreviousStockQty = previousProduct?.StockQty ?? 0
            };

            await _queue.EnqueueProductChangeAsync(message);
        }


        public async Task DeleteProductAsync(string rowKey) =>
            await _table.DeleteEntityAsync("Retail", rowKey);

    }
}
