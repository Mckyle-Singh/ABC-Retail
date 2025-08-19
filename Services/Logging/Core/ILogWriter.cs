namespace ABC_Retail.Services.Logging.Core
{
    public interface ILogWriter
    {
        Task WriteAsync(string domain, string message);

    }
}
