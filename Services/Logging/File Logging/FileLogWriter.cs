using ABC_Retail.Services.Logging.Core;

namespace ABC_Retail.Services.Logging.File_Logging
{
    public class FileLogWriter:ILogWriter
    {
        private readonly ILogPathResolver _pathResolver;

        public FileLogWriter(ILogPathResolver pathResolver)
        {
            _pathResolver = pathResolver;
        }

        public async Task WriteAsync(string domain, string message)
        {
            var filePath = _pathResolver.ResolvePath(domain);
            var logEntry = $"{DateTime.UtcNow:o} - {message}{Environment.NewLine}";
            await File.AppendAllTextAsync(filePath, logEntry);
        }

    }
}
