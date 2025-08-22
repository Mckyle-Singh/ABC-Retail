namespace ABC_Retail.Models.ViewModels
{
    public class ProductChangeViewModel
    {
        public string ProductName { get; set; }
        public string ChangeType { get; set; } // e.g., "PriceUpdated", "StockDecremented"
        public string Details { get; set; }
        public DateTime Timestamp { get; set; }

    }
}
