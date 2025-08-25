namespace ABC_Retail.Models.DTOs
{
    public class StockReminderQueueMessageDto
    {
        public string ProductId { get; set; }            // Unique identifier
        public string ProductName { get; set; }          // For human-readable logs
        public int CurrentStock { get; set; }            // Remaining quantity
        public int Threshold { get; set; }               // Trigger level
        public DateTime TriggeredAt { get; set; }        // UTC timestamp
        public string? UrgencyLevel { get; set; }        // Optional: "Low", "Medium", "High"
        public string? CorrelationId { get; set; }
    }
}
