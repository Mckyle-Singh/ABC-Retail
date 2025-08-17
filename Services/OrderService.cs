using ABC_Retail.Models;
using Azure;
using Azure.Data.Tables;
using Newtonsoft.Json;

namespace ABC_Retail.Services
{
    public class OrderService
    {
        private readonly TableClient _orderTable;
        private readonly TableClient _customerTable;


        public OrderService(TableServiceClient client)
        {
            _orderTable = client.GetTableClient("Orders");
            _orderTable.CreateIfNotExists();
            _customerTable = client.GetTableClient("Customers");


        }

        public async Task<string> PlaceOrderAsync(string customerId, List<CartItem> cartItems, double total)
        {
            var orderId = Guid.NewGuid().ToString();

            var order = new Order
            {
                PartitionKey = customerId,
                RowKey = orderId,
                Timestamp = DateTimeOffset.UtcNow,
                Status = "Placed",
                CartSnapshotJson = JsonConvert.SerializeObject(cartItems),
                TotalAmount = total,
                Email = customerId, // assuming customerId is the email
               // CustomerName = customerName,

            };

            await _orderTable.AddEntityAsync(order);
            return orderId;
        }

        public async Task<Order> GetOrderByIdAsync(string customerId, string orderId)
        {
            var response = await _orderTable.GetEntityAsync<Order>(customerId, orderId);
            return response.Value;
        }

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            var orders = new List<Order>();

            await foreach (var order in _orderTable.QueryAsync<Order>())
            {
                order.Email = order.PartitionKey;
                var normalizedEmail = order.Email.Trim().ToLower();
                try
                {
                    var customerResponse = await _customerTable.GetEntityAsync<Customer>("Customer", normalizedEmail);
                    order.CustomerName = customerResponse.Value.FullName;
                }
                catch (RequestFailedException)
                {
                    order.CustomerName = "Unknown";
                }

                orders.Add(order);
            }

            return orders;
        }
    }
}
