using ABC_Retail.Services.Logging.Core;
using Azure.Storage.Files.Shares;
using Azure;
using System.Text;

namespace ABC_Retail.Services.Logging.AzureFileShare_Logging
{
    public class AzureFileShareLogWriter : ILogWriter
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
            // 1) Resolve path and clients
            var relativePath = _pathResolver.ResolvePath(domain);
            var segments = relativePath.Split('/');
            var directory = string.Join("/", segments.Take(segments.Length - 1));
            var fileName = segments.Last();

            var dirClient = _shareClient.GetDirectoryClient(directory);
            await dirClient.CreateIfNotExistsAsync();
            var fileClient = dirClient.GetFileClient(fileName);

            // 2) Download existing content (if any)
            byte[] existing = Array.Empty<byte>();
            if (await fileClient.ExistsAsync())
            {
                var download = await fileClient.DownloadAsync();
                using var src = download.Value.Content;
                using var ms = new MemoryStream();
                await src.CopyToAsync(ms);
                existing = ms.ToArray();
            }

            // 3) Compose new line with ISO timestamp prefix
            var timestamp = DateTime.UtcNow.ToString("O");
            var newLine = $"{timestamp} - {message}{Environment.NewLine}";
            var newBytes = Encoding.UTF8.GetBytes(newLine);

            // 4) Combine old + new bytes
            var combined = new byte[existing.Length + newBytes.Length];
            Buffer.BlockCopy(existing, 0, combined, 0, existing.Length);
            Buffer.BlockCopy(newBytes, 0, combined, existing.Length, newBytes.Length);

            // 5) Delete old file (if exists), create new with correct length, then upload
            if (await fileClient.ExistsAsync())
                await fileClient.DeleteAsync();

            await fileClient.CreateAsync(combined.Length);

            using var buffer = new MemoryStream(combined);
            await fileClient.UploadRangeAsync(
                new HttpRange(0, combined.Length),
                buffer);
        }
    }

}
