using ABC_Retail.Models.DTOs;
using Azure.Storage.Queues;
using Newtonsoft.Json;
using System.Text;


namespace ABC_Retail.Services
{
    public class OrderPlacedQueueService
    {
        private readonly QueueClient _queueClient;

        public OrderPlacedQueueService(string? connectionString, string queueName)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Storage connection string is missing.");

            _queueClient = new QueueClient(connectionString, queueName);
            _queueClient.CreateIfNotExists(); // Ensures queue exists

        }
        public async Task EnqueueOrderAsync(OrderPlacedQueueMessageDto message)
        {
            var payload = JsonConvert.SerializeObject(message);
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
            await _queueClient.SendMessageAsync(base64);
        }


    }
}
