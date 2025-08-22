using ABC_Retail.Models;
using ABC_Retail.Models.DTOs;
using ABC_Retail.Services.Queues;
using Azure;
using Azure.Data.Tables;
using Newtonsoft.Json;

namespace ABC_Retail.Services
{
    public class OrderService
    {
        private readonly TableClient _orderTable;
        private readonly TableClient _customerTable;
        private readonly OrderPlacedQueueService _queueService;
        private readonly TableClient _productTable;



        public OrderService(TableServiceClient client, OrderPlacedQueueService queueService)
        {
            _orderTable = client.GetTableClient("Orders");
            _orderTable.CreateIfNotExists();
            _customerTable = client.GetTableClient("Customers");
            _queueService = queueService;
            _productTable = client.GetTableClient("Products");
        }

        public async Task<string> PlaceOrderAsync(string customerId, List<CartItem> cartItems, double total)
        {
            // 🔍 Diagnostic: Check quantities before serialization
            foreach (var item in cartItems)
            {
                Console.WriteLine($"[Snapshot] Product: {item.RowKey}, Quantity: {item.Quantity}");
            }

            var cartSnapshotJson = JsonConvert.SerializeObject(cartItems);
            var stockUpdated = await DecrementStockAsync(cartSnapshotJson);
            if (!stockUpdated)
            {
                throw new InvalidOperationException("Insufficient stock for one or more items.");
            }

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

            var message = new OrderPlacedQueueMessageDto
            {
                OrderId = orderId,
                CustomerId = customerId,
                Status = "Placed",
                TotalAmount = total,
                CartSnapshotJson = JsonConvert.SerializeObject(cartItems),
                Email = customerId,
                PlacedAt = DateTime.UtcNow
            };

            await _queueService.EnqueueOrderAsync(message);

            return orderId;
        }

        private async Task<bool> DecrementStockAsync(string cartSnapshotJson)
        {
            var cartItems = JsonConvert.DeserializeObject<List<CartItem>>(cartSnapshotJson);

            foreach (var item in cartItems)
            {
                var response = await _productTable.GetEntityAsync<Product>("Retail", item.RowKey);
                var product = response.Value;

                if (product.StockQty < item.Quantity)
                {
                    // Optional: log insufficient stock
                    return false;
                }

                product.StockQty -= item.Quantity;

                await _productTable.UpdateEntityAsync(product, product.ETag, TableUpdateMode.Replace);
            }

            return true;
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
