using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace ABC_Retail.Models
{
    public class Product:ITableEntity
    {
        [Required]
        public string PartitionKey { get; set; } = "Retail";

        [Required]
        public string RowKey { get; set; }

        [Required(ErrorMessage = "Product name is required")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Category is required")]
        public string Category { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero")]
        public double Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity must be zero or more")]
        public int StockQty { get; set; }

        public string? ImageUrl { get; set; }

        [StringLength(500, ErrorMessage = "Description must be under 500 characters")]
        public string Description { get; set; }

        [IgnoreDataMember]
        public IFormFile? ImageFile { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

    }
}
