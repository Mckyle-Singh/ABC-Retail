using ABC_Retail.Models;
using ABC_Retail.Models.DTOs;
using ABC_Retail.Services.Queues;
using Azure;
using Azure.Data.Tables;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ABC_Retail.Services
{
    public class CustomerService
    {
        private readonly TableClient _table;
        private readonly CustomerRegistrationQueueService _queueService;
        public CustomerService(TableServiceClient serviceClient, CustomerRegistrationQueueService queueService)
        {
            _table = serviceClient.GetTableClient("Customers");
            _table.CreateIfNotExists(); // Safe init, same as ProductService
            _queueService = queueService;
        }

        // Register a new customer
        public async Task<bool> RegisterCustomerAsync(Customer customer)
        {
            var existing = await _table.GetEntityIfExistsAsync<Customer>(
                customer.PartitionKey, customer.RowKey);

            if (existing.HasValue) return false;

            customer.PasswordHash = HashPassword(customer.PasswordHash.Trim());
            customer.RegisteredOn = DateTime.UtcNow;
            customer.IsActive = true;

            // Send message to the customer registration queue
            var messageDto = new CustomerRegistrationQueueMessageDTO
            {
                FullName = customer.FullName,
                Email = customer.Email,
                PasswordHash = customer.PasswordHash,
                RegisteredOn = customer.RegisteredOn
            };

            // Log the message so you can see it in console
            Console.WriteLine("Sending customer registration message to queue:");
            var messageJson = JsonSerializer.Serialize(messageDto);
            Console.WriteLine(messageJson);

            await _queueService.SendCustomerRegistrationAsync(JsonSerializer.Serialize(messageDto));

            // Optionally log after it is added
            Console.WriteLine($"Customer {customer.FullName} added to Azure Table and queue message sent.");


            await _table.AddEntityAsync(customer);
            return true;
        }

        // Login attempt
        public async Task<Customer?> LoginCustomerAsync(string rowKey, string password)
        {
            try
            {
                var response = await _table.GetEntityAsync<Customer>("Customer", rowKey);
                var customer = response.Value;

                bool isValid = VerifyPassword(password.Trim(), customer.PasswordHash);

                if (isValid && customer.IsActive)
                    return customer;

                return null;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }
        private string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            return Convert.ToBase64String(sha.ComputeHash(bytes));
        }

        private bool VerifyPassword(string input, string storedHash)
        {
            string enteredHash = HashPassword(input.Trim());

            Console.WriteLine("Entered password hash: " + enteredHash);
            Console.WriteLine("Stored password hash: " + storedHash);

            return enteredHash == storedHash;
        }

        public async Task<List<Customer>> GetActiveCustomersAsync()
        {
            var customers = new List<Customer>();

            await foreach (var customer in _table.QueryAsync<Customer>(c => c.PartitionKey == "Customer" && c.IsActive))
            {
                customers.Add(customer);
            }

            return customers;
        }


    }

}

