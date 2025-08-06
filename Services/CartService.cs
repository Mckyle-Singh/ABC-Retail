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
            var queryResults = _table.QueryAsync<TableEntity>(item => item.PartitionKey == normalizedEmail);

            var cartItems = new List<CartItem>();
            await foreach (var entity in queryResults)
            {
                var cartItem = new CartItem
                {
                    PartitionKey = entity.PartitionKey,
                    RowKey = entity.RowKey,
                    ProductName = entity.GetString("ProductName"),
                    Quantity = entity.GetInt32("Quantity") ?? 0,
                    Price = Convert.ToDecimal(entity["Price"]),  // 🔥 This is the critical fix
                    AddedOn = entity.GetDateTime("AddedOn") ?? DateTime.MinValue,
                    ETag = entity.ETag,
                    Timestamp = entity.Timestamp
                };

                cartItems.Add(cartItem);
            }

            return cartItems;
        }

        public async Task ClearCartAsync(string customerEmail)
        {
            var normalizedEmail = customerEmail.ToLower().Trim();
            var queryResults = _table.QueryAsync<TableEntity>(item => item.PartitionKey == normalizedEmail);

            var batch = new List<TableTransactionAction>();

            await foreach (var entity in queryResults)
            {
                batch.Add(new TableTransactionAction(TableTransactionActionType.Delete, entity));
            }

            if (batch.Any())
            {
                // Azure Table Storage allows max 100 operations per batch
                foreach (var chunk in batch.Chunk(100))
                {
                    await _table.SubmitTransactionAsync(chunk.ToList());
                }
            }
        }

    }
}
