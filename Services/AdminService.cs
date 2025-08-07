using ABC_Retail.Models;
using Azure;
using Azure.Data.Tables;
using System.Security.Cryptography;
using System.Text;

namespace ABC_Retail.Services
{
    public class AdminService
    {
        private readonly TableClient _table;

        public AdminService(TableServiceClient serviceClient)
        {
            _table = serviceClient.GetTableClient("Admins");
            _table.CreateIfNotExists();

        }

        public async Task<Admin?> LoginAdminAsync(string email, string password)
        {
            try
            {
                var response = await _table.GetEntityAsync<Admin>("Admin", email.ToLower());
                var admin = response.Value;

                bool isValid = VerifyPassword(password.Trim(), admin.PasswordHash);
                return (isValid && admin.IsActive) ? admin : null;
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
            return enteredHash == storedHash;
        }



    }
}
