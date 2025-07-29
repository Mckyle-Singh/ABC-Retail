using Azure;
using Azure.Data.Tables;

namespace ABC_Retail.Models
{
    public class Product:ITableEntity
    {
        public string PartitionKey { get; set; } = "Retail";           // Logical grouping
        public string RowKey { get; set; }                             // Unique ID (SKU or Guid)
        public string Name { get; set; }                               // Product name
        public string Category { get; set; }                           // e.g. Electronics, Apparel
        public double Price { get; set; }                              // Unit price
        public int StockQty { get; set; }                              // Inventory count
        public string ImageUrl { get; set; }                           // Blob URI of image
        public string Description { get; set; }                        // Optional product info
        public DateTimeOffset? Timestamp { get; set; }                 // Required for ITableEntity
        public ETag ETag { get; set; }
    }
}
