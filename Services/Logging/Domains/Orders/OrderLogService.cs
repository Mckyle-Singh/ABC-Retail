using ABC_Retail.Models;
using ABC_Retail.Services.Logging.Core;
using Newtonsoft.Json;

namespace ABC_Retail.Services.Logging.Domains.Orders
{
    public class OrderLogService
    {
        private readonly ILogWriter _logWriter;

        public OrderLogService(ILogWriter logWriter)
        {
            _logWriter = logWriter;
        }
        public async Task LogOrderCheckedOutAsync(Order order)
        {
            var logEntry = new
            {
                orderId = order.RowKey,
                customerId = order.PartitionKey,
                email = order.Email,
                total = order.TotalAmount,
                status = order.Status,
                placedAt = order.Timestamp?.UtcDateTime,
                cartSnapshot = order.CartSnapshotJson
            };

            var json = JsonConvert.SerializeObject(logEntry, Formatting.None);
            await _logWriter.WriteAsync("orders", json);
        }

    }
}
