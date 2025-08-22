namespace ABC_Retail.Models.ViewModels
{

    public class AdminDashboardViewModel
    {
        public class ProductChange
        {
            public string ProductName { get; set; }
            public string ChangeType { get; set; } // "Create", "Update", "Delete"
            public DateTime Timestamp { get; set; }
            public string Details { get; set; } // Optional: price/stock changes
        }

        public string AdminEmail { get; set; }
        public List<ProductChange> ProductChanges { get; set; } = new();

    }
}
