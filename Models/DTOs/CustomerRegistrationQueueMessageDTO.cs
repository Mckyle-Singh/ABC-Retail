namespace ABC_Retail.Models.DTOs
{
    public class CustomerRegistrationQueueMessageDTO
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public DateTime RegisteredOn { get; set; }
    }
}
