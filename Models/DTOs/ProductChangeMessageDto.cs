namespace ABC_Retail.Models.DTOs
{
    public class ProductChangeMessageDto
    {
        public string PartitionKey { get; set; } = "Retail";
        public string RowKey { get; set; }
        public string ChangeType { get; set; } // "Create", "Update", "Delete"
        public DateTime Timestamp { get; set; }

        public string Name { get; set; }
        public string Category { get; set; }
        public double Price { get; set; }
        public int StockQty { get; set; }

        public string? ImageUrl { get; set; }
        public string Description { get; set; }

        // Optional: Previous values for audit/diffing
        public double? PreviousPrice { get; set; }
        public int? PreviousStockQty { get; set; }


    }

}

