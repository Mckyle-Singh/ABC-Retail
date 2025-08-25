namespace ABC_Retail.Services.Logging.Core
{
    public interface ILogReader
    {
     Task<IEnumerable<string>> ReadLinesAsync(string domain);

    }
}
