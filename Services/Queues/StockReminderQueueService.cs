using ABC_Retail.Models.DTOs;
using Azure.Storage.Queues;
using System.Text;
using System.Text.Json;

namespace ABC_Retail.Services.Queues
{
    public class StockReminderQueueService
    {
        private readonly QueueClient _queueClient;
        private readonly ILogger<StockReminderQueueService> _logger;

        public StockReminderQueueService(IConfiguration config, ILogger<StockReminderQueueService> logger)
        {
            _logger = logger;

            var connectionString = config["AzureStorage:ConnectionString"];
            var queueName = config["AzureStorage:StockReminderQueueName"];

            _queueClient = new QueueClient(connectionString, queueName);
            _queueClient.CreateIfNotExists();
        }

        public async Task EnqueueReminderAsync(StockReminderQueueMessageDto messageDto)
        {
            var payload = JsonSerializer.Serialize(messageDto);
            _logger.LogInformation("Enqueuing stock reminder: {Payload}", payload);

            var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
            await _queueClient.SendMessageAsync(encoded);
        }

    }
}
