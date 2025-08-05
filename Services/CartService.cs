using ABC_Retail.Models;
using Azure.Data.Tables;

namespace ABC_Retail.Services
{
    public class CartService
    {
        private readonly TableClient _table;

        public CartService(TableServiceClient serviceClient)
        {
            _table = serviceClient.GetTableClient("CartItems");
            _table.CreateIfNotExists();
        }

        public async Task AddToCartAsync( Product product, int quantity)
        {
            var item = new CartItem
            {
                PartitionKey = "guest@demo.local",
                RowKey = product.RowKey,             // use RowKey as product ID
                ProductName = product.Name,
                Quantity = quantity,
                Price = (decimal)product.Price,               
                AddedOn = DateTime.UtcNow

            };

            await _table.UpsertEntityAsync(item); // update if exists
        }

    }
}
