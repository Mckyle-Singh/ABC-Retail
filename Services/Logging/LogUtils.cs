namespace ABC_Retail.Services.Logging
{
    public class LogUtils
    {
        public static DateTime ExtractTimestamp(string line)
        {
            var timestampPart = line.Split(" - ")[0];
            return DateTime.TryParse(timestampPart, out var parsed)
                ? parsed
                : DateTime.MinValue;
        }

        public static string FormatTimestamp(DateTime timestamp)
        {
            return timestamp.ToString("dd MMM yyyy, HH:mm"); // e.g. "25 Aug 2025, 14:09"
        }

    }
}
