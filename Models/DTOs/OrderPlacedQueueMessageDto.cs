namespace ABC_Retail.Models.DTOs
{
    public class OrderPlacedQueueMessageDto
    {
        public string OrderId { get; set; }           // From RowKey
        public string CustomerId { get; set; }        // From PartitionKey
        public string Status { get; set; }            // "Placed", "Processing", etc.
        public double TotalAmount { get; set; }
        public string CartSnapshotJson { get; set; }  // Serialized cart items
        public string CustomerName { get; set; }
        public string Email { get; set; }
        public DateTime PlacedAt { get; set; }

    }
}
