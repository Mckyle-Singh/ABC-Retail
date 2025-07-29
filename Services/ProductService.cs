using ABC_Retail.Models;
using Azure.Data.Tables;

namespace ABC_Retail.Services
{
    public class ProductService
    {
        private readonly TableClient _table;

        public ProductService(TableServiceClient serviceClient)
        {
            _table = serviceClient.GetTableClient("Products");
            _table.CreateIfNotExists(); // Safe init
        }

        public async Task AddProductAsync(Product product) =>
            await _table.AddEntityAsync(product);

        public async Task<List<Product>> GetProductsAsync()
        {
            var items = new List<Product>();
            await foreach (Product p in _table.QueryAsync<Product>())
                items.Add(p);
            return items;
        }

        public async Task UpdateProductAsync(Product product) =>
            await _table.UpdateEntityAsync(product, product.ETag);

        public async Task DeleteProductAsync(string rowKey) =>
            await _table.DeleteEntityAsync("Retail", rowKey);

    }
}
