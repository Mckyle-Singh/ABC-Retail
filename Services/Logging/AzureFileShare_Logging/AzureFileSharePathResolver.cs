using ABC_Retail.Services.Logging.Core;
using Azure.Storage.Files.Shares;

namespace ABC_Retail.Services.Logging.AzureFileShare_Logging
{
    public class AzureFileSharePathResolver : ILogPathResolver
    {
        private readonly ShareClient _shareClient;

        public AzureFileSharePathResolver(
            ShareServiceClient shareServiceClient,
            string shareName  // e.g. from env AZURE_FILE_SHARE_NAME
        )
        {
            if (string.IsNullOrWhiteSpace(shareName))
                throw new ArgumentException(
                    "Azure File Share name is not configured.",
                    nameof(shareName));

            _shareClient = shareServiceClient.GetShareClient(shareName);
            _shareClient.CreateIfNotExists();  // ensure the share exists
        }

        public string ResolvePath(string domain)
        {
            var now = DateTime.UtcNow;
            var year = now.Year.ToString();
            var month = now.Month.ToString("D2");
            var directory = $"{domain}/{year}/{month}";

            // ensure year/month directory exists
            var dirClient = _shareClient.GetDirectoryClient(directory);
            dirClient.CreateIfNotExists();

            // daily log filename
            var fileName = $"{now:yyyy-MM-dd}.log";
            return $"{directory}/{fileName}";
        }


    }
}
