namespace ABC_Retail.Models.DTOs
{
    public class ProductChangeMessageDto
    {
        public string ProductId { get; set; }
        public string Action { get; set; } // "Created", "Updated", "Deleted"
        public Dictionary<string, (string OldValue, string NewValue)>? Changes { get; set; }
        public string ChangedBy { get; set; }
        public DateTime Timestamp { get; set; }

    }
}
