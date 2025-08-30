using ABC_Retail.Services.Logging.Core;
using Azure.Storage.Files.Shares;

namespace ABC_Retail.Services.Logging.AzureFileShare_Logging
{
    public class AzureFileShareLogReader:ILogReader
    {
        private readonly ShareClient _shareClient;
        private readonly ILogPathResolver _pathResolver;

        public AzureFileShareLogReader(
            ShareServiceClient shareServiceClient,
            string shareName,
            ILogPathResolver pathResolver)
        {
            if (string.IsNullOrWhiteSpace(shareName))
                throw new ArgumentException(
                    "Azure File Share name is not configured.",
                    nameof(shareName));

            // Ensure we can talk to the correct share
            _shareClient = shareServiceClient.GetShareClient(shareName);
            _shareClient.CreateIfNotExists();

            _pathResolver = pathResolver;
        }

        public async Task<IEnumerable<string>> ReadLinesAsync(string domain)
        {
            // 1) Resolve path (e.g. "orders/2025/08/2025-08-30.log")
            var relativePath = _pathResolver.ResolvePath(domain);
            var segments = relativePath.Split('/');
            var directory = string.Join("/", segments.Take(segments.Length - 1));
            var fileName = segments.Last();

            // 2) Point at directory and file
            var dirClient = _shareClient.GetDirectoryClient(directory);
            var fileClient = dirClient.GetFileClient(fileName);

            // 3) If file doesn’t exist, return empty
            if (!await fileClient.ExistsAsync())
                return Enumerable.Empty<string>();

            // 4) Download content
            var download = await fileClient.DownloadAsync();
            using var stream = download.Value.Content;
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();

            // 5) Split into lines
            return content
                .Split(new[] { Environment.NewLine }, StringSplitOptions.None)
                .Where(line => !string.IsNullOrEmpty(line));
        }
    }
}
