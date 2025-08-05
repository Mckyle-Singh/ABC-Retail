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

        public async Task AddToCartAsync( Product product, int quantity, string customerEmail)
        {
            var item = new CartItem
            {
                PartitionKey = customerEmail.ToLower().Trim(),  
                RowKey = product.RowKey,             // use RowKey as product ID
                ProductName = product.Name,
                Quantity = quantity,
                Price = (decimal)product.Price,               
                AddedOn = DateTime.UtcNow

            };

            await _table.UpsertEntityAsync(item); // update if exists
        }

        public async Task<List<CartItem>> GetCartAsync(string customerEmail)
        {
            var normalizedEmail = customerEmail.ToLower().Trim();
            var queryResults = _table.QueryAsync<CartItem>(item => item.PartitionKey == normalizedEmail);

            var cartItems = new List<CartItem>();
            await foreach (var item in queryResults)
            {
                cartItems.Add(item);
            }

            return cartItems;
        }


    }
}
