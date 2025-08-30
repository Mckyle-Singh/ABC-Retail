using ABC_Retail.Services.Logging.Core;
using Azure.Storage.Files.Shares;
using Azure;
using System.Text;

namespace ABC_Retail.Services.Logging.AzureFileShare_Logging
{
    public class AzureFileShareLogWriter:ILogWriter
    {
        private readonly ShareClient _shareClient;
        private readonly ILogPathResolver _pathResolver;

        public AzureFileShareLogWriter(
            ShareServiceClient shareServiceClient,
            string shareName,
            ILogPathResolver pathResolver)
        {
            if (string.IsNullOrWhiteSpace(shareName))
                throw new ArgumentException(
                    "Azure File Share name is not configured.",
                    nameof(shareName));

            // ensure the File Share exists
            _shareClient = shareServiceClient.GetShareClient(shareName);
            _shareClient.CreateIfNotExists();

            _pathResolver = pathResolver;
        }

        public async Task WriteAsync(string domain, string message)
        {
            // 1) Determine relative path like "orders/2025/08/2025-08-30.log"
            var relativePath = _pathResolver.ResolvePath(domain);
            var segments = relativePath.Split('/');
            var directory = string.Join("/", segments.Take(segments.Length - 1));
            var fileName = segments.Last();

            // 2) Ensure the directory exists
            var dirClient = _shareClient.GetDirectoryClient(directory);
            await dirClient.CreateIfNotExistsAsync();

            // 3) Get file client
            var fileClient = dirClient.GetFileClient(fileName);

            // 4) Prepare the log entry bytes
            var logEntry = $"{DateTime.UtcNow:o} - {message}{Environment.NewLine}";
            var contentBytes = Encoding.UTF8.GetBytes(logEntry);
            var length = contentBytes.Length;

            if (!await fileClient.ExistsAsync())
            {
                // First write: create and upload
                await fileClient.CreateAsync(length);
                using var ms = new MemoryStream(contentBytes);
                await fileClient.UploadAsync(ms);
            }
            else
            {
                // Append: get current length, then upload range
                var props = await fileClient.GetPropertiesAsync();
                long offset = props.Value.ContentLength;

                using var ms = new MemoryStream(contentBytes);
                var range = new HttpRange(offset, length);
                await fileClient.UploadRangeAsync(range, ms);
            }

        }
    }
}
