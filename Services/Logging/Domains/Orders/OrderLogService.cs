using ABC_Retail.Services.Logging.Core;

namespace ABC_Retail.Services.Logging.Domains.Orders
{
    public class OrderLogService
    {
        private readonly ILogWriter _logWriter;

        public OrderLogService(ILogWriter logWriter)
        {
            _logWriter = logWriter;
        }

        public async Task LogOrderCheckedOutAsync(string orderId, string userId, IEnumerable<string> productIds, double totalAmount, string campaignTag = null)
        {
            var productList = string.Join(", ", productIds);
            var message = $"✅ Order <strong>{orderId}</strong> checked out by <strong>{userId}</strong> — Products: [{productList}], Total: <strong>{totalAmount:C}</strong>" +
                          (campaignTag != null ? $" — Campaign: <em>{campaignTag}</em>" : "");

            await _logWriter.WriteAsync(LogDomain.Orders, message);
        }

    }
}
