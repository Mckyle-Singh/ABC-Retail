namespace ABC_Retail.Models.ViewModels
{
    public class LowStockReminderViewModel
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public int CurrentStock { get; set; }
        public int Threshold { get; set; }
        public string UrgencyLevel { get; set; }
        public string TriggeredAtFormatted { get; set; }

    }
}
