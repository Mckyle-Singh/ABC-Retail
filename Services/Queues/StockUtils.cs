namespace ABC_Retail.Services.Queues
{
    public class StockUtils
    {
        public static string ClassifyUrgency(int currentStock, int threshold)
        {
            var ratio = (double)currentStock / threshold;

            if (ratio <= 0.25) return "Critical";
            if (ratio <= 0.5) return "High";
            if (ratio <= 0.75) return "Medium";
            return "Low";
        }

    }
}
