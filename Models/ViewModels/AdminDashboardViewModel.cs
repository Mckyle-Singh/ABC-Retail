namespace ABC_Retail.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public string AdminEmail { get; set; }
        public List<string> ProductChangeFeed { get; set; }
        public List<LowStockReminderViewModel> Reminders { get; set; } = new();

    }
}
