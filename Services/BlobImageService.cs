using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ABC_Retail.Services
{
    public class BlobImageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName = "product-images";


        public BlobImageService(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
        }
        private async Task<BlobContainerClient> GetOrCreateContainerAsync()
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
            return containerClient;
        }
        public async Task<string> UploadImageAsync(Stream imageStream, string originalFileName, string contentType)
        {

            var containerClient = await GetOrCreateContainerAsync();

            // Generate a unique filename to avoid collisions
            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(originalFileName)}";
            var blobClient = containerClient.GetBlobClient(uniqueFileName);

            // Upload the image with content type
            await blobClient.UploadAsync(imageStream, new BlobHttpHeaders { ContentType = contentType });

            // Return the public URL
            return blobClient.Uri.ToString();
        }

    }
}
