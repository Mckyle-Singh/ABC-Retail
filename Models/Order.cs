using Azure;
using Azure.Data.Tables;

namespace ABC_Retail.Models
{
    public class Order:ITableEntity
    {
        public string PartitionKey { get; set; } // CustomerId
        public string RowKey { get; set; }       // OrderId (Guid or timestamp-based)
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string Status { get; set; }       // "Placed", "Processing", etc.
        public string CartSnapshotJson { get; set; } // Serialized cart items
        public double TotalAmount { get; set; }

        // Optional metadata
        public string CustomerName { get; set; }
        public string Email { get; set; }


    }
}
