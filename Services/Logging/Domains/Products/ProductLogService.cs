using ABC_Retail.Services.Logging.Core;

namespace ABC_Retail.Services.Logging.Domains.Products
{
    public class ProductLogService
    {
        private readonly ILogWriter _logWriter;

        public ProductLogService(ILogWriter logWriter)
        {
            _logWriter = logWriter;
        }

        public async Task LogProductAddedAsync(string productId, string name, double price, int stockQty)
        {
            var message = $"🟢 <strong>{name}</strong> added — Price: <strong>{price}</strong>, Stock: <strong>{stockQty}</strong>";
            await _logWriter.WriteAsync(LogDomain.Products, message);

        }

        public async Task LogProductUpdatedAsync(string productId, string name, double previousPrice, double newPrice, int previousStockQty, int newStockQty)
        {
            var priceDiff = previousPrice != newPrice
                ? $"Price: <s>{previousPrice}</s> → <strong>{newPrice}</strong>"
                : null;

            var stockDiff = previousStockQty != newStockQty
                ? $"Stock: <s>{previousStockQty}</s> → <strong>{newStockQty}</strong>"
                : null;

            var changes = string.Join(", ", new[] { priceDiff, stockDiff }.Where(x => x != null));
            var message = $"🟡 <strong>{name}</strong> updated — {changes}";
            await _logWriter.WriteAsync(LogDomain.Products, message);

        }

        public async Task LogProductDeletedAsync(string productId, string name, double price, int stockQty)
        {
            var message = $"🔴 <strong>{name}</strong> deleted — Last known Price: <strong>{price}</strong>, Stock: <strong>{stockQty}</strong>";
            await _logWriter.WriteAsync(LogDomain.Products, message);
        }


    }
}
