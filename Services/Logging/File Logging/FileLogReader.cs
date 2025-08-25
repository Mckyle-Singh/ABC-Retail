using ABC_Retail.Services.Logging.Core;

namespace ABC_Retail.Services.Logging.File_Logging
{
    public class FileLogReader: ILogReader
    {
        private readonly ILogPathResolver _pathResolver;

        public FileLogReader(ILogPathResolver pathResolver)
        {
            _pathResolver = pathResolver;
        }

        public async Task<IEnumerable<string>> ReadLinesAsync(string domain)
        {
            var path = _pathResolver.ResolvePath(domain);
            if (!File.Exists(path)) return Enumerable.Empty<string>();

            return await File.ReadAllLinesAsync(path);
        }

    }
}
