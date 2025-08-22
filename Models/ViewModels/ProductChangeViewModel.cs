namespace ABC_Retail.Models.ViewModels
{
    public class ProductChangeViewModel
    {
        public string ProductId { get; set; }
        public string ChangeType { get; set; } // e.g., "PriceUpdated", "StockDecremented"
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }

    }
}
