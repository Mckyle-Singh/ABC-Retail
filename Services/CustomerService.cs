using ABC_Retail.Models;
using Azure;
using Azure.Data.Tables;
using System.Security.Cryptography;
using System.Text;

namespace ABC_Retail.Services
{
    public class CustomerService
    {
        private readonly TableClient _table;
        public CustomerService(TableServiceClient serviceClient)
        {
            _table = serviceClient.GetTableClient("Customers");
            _table.CreateIfNotExists(); // Safe init, same as ProductService
        }

        // Register a new customer
        public async Task<bool> RegisterCustomerAsync(Customer customer)
        {
            var existing = await _table.GetEntityIfExistsAsync<Customer>(
                customer.PartitionKey, customer.RowKey);

            if (existing.HasValue) return false;

            customer.PasswordHash = HashPassword(customer.PasswordHash);
            customer.RegisteredOn = DateTime.UtcNow;
            customer.IsActive = true;

            await _table.AddEntityAsync(customer);
            return true;
        }

        // Login attempt
        public async Task<Customer?> LoginCustomerAsync(string rowKey, string password)
        {
            try
            {
                var response = await _table.GetEntityAsync<Customer>("Customer", rowKey);
                if (VerifyPassword(password, response.Value.PasswordHash))
                    return response.Value;

                return null;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        // Password handling
        private string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            return Convert.ToBase64String(sha.ComputeHash(bytes));
        }

        private bool VerifyPassword(string input, string storedHash) =>
            HashPassword(input) == storedHash;
    }

}

