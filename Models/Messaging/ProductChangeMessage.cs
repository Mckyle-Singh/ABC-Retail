namespace ABC_Retail.Models.Messaging
{
    public class ProductChangeMessage
    {
        public string ProductName { get; set; }
        public string ChangeType { get; set; } // "Create", "Update", "Delete"
        public DateTime Timestamp { get; set; }
        public string Details { get; set; } // Optional: deltas, notes

    }
}
