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
        while (!stoppingToken.IsCancellationRequested)
        {
            var message = await _reader.ReadNextMessageAsync(stoppingToken);
            if (message != null)
            {
                _logger.LogInformation("Received product change: {@Message}", message);

                var viewModel = new ProductChangeViewModel
                {
                    ProductName = message.ProductName,
                    ChangeType = message.ChangeType,
                    Timestamp = message.Timestamp,
                    Details = message.Details

                };

                _store.Add(viewModel);
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken); // Throttle reads
        }
    }


    }
}
