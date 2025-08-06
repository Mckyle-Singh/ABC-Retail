using ABC_Retail.Models;
using Azure.Data.Tables;
using Newtonsoft.Json;

namespace ABC_Retail.Services
{
    public class OrderService
    {
        private readonly TableClient _orderTable;

        public OrderService(TableServiceClient client)
        {
            _orderTable = client.GetTableClient("Orders");
        }

        public async Task<string> PlaceOrderAsync(string customerId, List<CartItem> cartItems, decimal total)
        {
            var orderId = Guid.NewGuid().ToString();
            var order = new Order
            {
                PartitionKey = customerId,
                RowKey = orderId,
                Timestamp = DateTimeOffset.UtcNow,
                Status = "Placed",
                CartSnapshotJson = JsonConvert.SerializeObject(cartItems),
                TotalAmount = total
            };

            await _orderTable.AddEntityAsync(order);
            return orderId;
        }

        public async Task<Order> GetOrderByIdAsync(string customerId, string orderId)
        {
            var response = await _orderTable.GetEntityAsync<Order>(customerId, orderId);
            return response.Value;
        }

    }
}
