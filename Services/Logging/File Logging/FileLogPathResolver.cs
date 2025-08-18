using ABC_Retail.Services.Logging.Core;

namespace ABC_Retail.Services.Logging.File_Logging
{
    public class FileLogPathResolver: ILogPathResolver
    {
        private readonly string _basePath;

        public FileLogPathResolver(string basePath)
        {
            _basePath = basePath;
        }
        public string ResolvePath(string domain)
        {
            var now = DateTime.UtcNow;
            var folder = Path.Combine(_basePath, domain, now.Year.ToString(), now.Month.ToString("D2"));
            Directory.CreateDirectory(folder);

            var fileName = $"{now:yyyy-MM-dd}.log";
            return Path.Combine(folder, fileName);
        }

    }
}
