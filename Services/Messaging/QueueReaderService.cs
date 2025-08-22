using ABC_Retail.Models.Messaging;
using Azure.Storage.Queues;
using System.Text.Json;

namespace ABC_Retail.Services.Messaging
{
    public class QueueReaderService:IQueueReaderService
    {
        private readonly QueueClient _queueClient;

        public QueueReaderService(QueueClient queueClient)
        {
            _queueClient = queueClient;
        }

        public async Task<ProductChangeMessage?> ReadNextMessageAsync(CancellationToken cancellationToken = default)
        {
            var response = await _queueClient.ReceiveMessageAsync(cancellationToken: cancellationToken);
            if (response?.Value?.MessageText is null)
                return null;

            try
            {
                return JsonSerializer.Deserialize<ProductChangeMessage>(response.Value.MessageText);
            }
            catch (JsonException ex)
            {
                // Log and optionally dead-letter or delete malformed message
                return null;
            }
        }

    }
}
