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

        public async Task LogProductAddedAsync(string productId)
        {
            var message = $"Product {productId} added to catalog";
            await _logWriter.WriteAsync(LogDomain.Products, message);
        }

        public async Task LogStockUpdatedAsync(string productId, int oldQty, int newQty)
        {
            var message = $"Stock updated for Product {productId}: {oldQty} → {newQty}";
            await _logWriter.WriteAsync(LogDomain.Products, message);
        }

        public async Task LogPriceChangedAsync(string productId, decimal oldPrice, decimal newPrice)
        {
            var message = $"Price updated for Product {productId}: {oldPrice:C} → {newPrice:C}";
            await _logWriter.WriteAsync(LogDomain.Products, message);
        }

    }
}
