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

        public async Task LogProductUpdatedAsync(string productId)
        {
            var message = $"Product {productId} details updated";
            await _logWriter.WriteAsync(LogDomain.Products, message);
        }

    }
}
