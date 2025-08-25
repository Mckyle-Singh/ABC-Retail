using ABC_Retail.Models.ViewModels;

namespace ABC_Retail.Services.Messaging
{
    public class ProductChangeProcessor:BackgroundService
    {
        private readonly IQueueReaderService _reader;
        private readonly IProductChangeStore _store;
        private readonly ILogger<ProductChangeProcessor> _logger;

        public ProductChangeProcessor(
            IQueueReaderService reader,
            IProductChangeStore store,
            ILogger<ProductChangeProcessor> logger)
        {
            _reader = reader;
            _store = store;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🔄 ProductChangeProcessor started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("🔍 Attempting to read next message from queue...");

                    var message = await _reader.ReadNextMessageAsync(stoppingToken);

                    if (message != null)
                    {
                        _logger.LogInformation("📥 Received message: {@Message}", message);

                        var viewModel = new ProductChangeViewModel
                        {
                            ProductName = message.ProductName,
                            ChangeType = message.ChangeType,
                            Timestamp = message.Timestamp,
                            Details = message.Details
                        };

                        _logger.LogInformation("📝 Storing change: {ProductName} | {ChangeType} | {Details}",
                            viewModel.ProductName, viewModel.ChangeType, viewModel.Details);

                        _store.Add(viewModel);

                        _logger.LogInformation("✅ Change stored successfully. Total changes: {Count}",
                            _store.LatestChanges.Count);
                    }
                    else
                    {
                        _logger.LogInformation("📭 No message received this cycle.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error while processing product change message.");
                }

                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken); // Throttle reads
            }

            _logger.LogInformation("🛑 ProductChangeProcessor stopping.");
        }


    }
}
