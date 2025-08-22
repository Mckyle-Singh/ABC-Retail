namespace ABC_Retail.Services.Messaging
{
    public class ProductChangeProcessor:BackgroundService
    {
        private readonly IQueueReaderService _reader;
        private readonly ILogger<ProductChangeProcessor> _logger;

        public ProductChangeProcessor(IQueueReaderService reader, ILogger<ProductChangeProcessor> logger)
        {
            _reader = reader;
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
                    // TODO: Add domain logic here (e.g., update inventory, notify services)
                }

                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken); // Throttle reads
            }
        }


    }
}
