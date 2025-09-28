using Azure.Storage.Queues;

namespace ABC_Retail.Services.Queues
{
    public class CustomerRegistrationQueueService
    {
        private readonly QueueClient _queueClient;

        public CustomerRegistrationQueueService(string connectionString, string queueName)
        {
            // Create the queue client
            _queueClient = new QueueClient(connectionString, queueName);
            // Ensure queue exists
            _queueClient.CreateIfNotExists();
        }

        // Send a message to the queue
        public async Task SendCustomerRegistrationAsync(string message)
        {
            await _queueClient.SendMessageAsync(message);
        }
    }
}
