using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Net.Http.Headers;

namespace ABC_Retail.Services
{
    public class BlobImageService
    {
        //private readonly BlobServiceClient _blobServiceClient;
        //private readonly string _containerName = "product-images";


        //public BlobImageService(BlobServiceClient blobServiceClient)
        //{
        //    _blobServiceClient = blobServiceClient;
        //}
        //private async Task<BlobContainerClient> GetOrCreateContainerAsync()
        //{
        //    var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        //    await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
        //    return containerClient;
        //}
        //public async Task<string> UploadImageAsync(Stream imageStream, string originalFileName, string contentType)
        //{

        //    var containerClient = await GetOrCreateContainerAsync();

        //    // Generate a unique filename to avoid collisions
        //    var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(originalFileName)}";
        //    var blobClient = containerClient.GetBlobClient(uniqueFileName);

        //    // Upload the image with content type
        //    await blobClient.UploadAsync(imageStream, new BlobHttpHeaders { ContentType = contentType });

        //    // Return the public URL
        //    return blobClient.Uri.ToString();
        //}

        // Hardcoded function URL (change to your local or Azure function URL)
        private readonly string _functionUrl = "http://localhost:7225/api/UploadImage";

        public BlobImageService()
        {
        }

        public async Task<string> UploadImageAsync(Stream imageStream, string originalFileName, string contentType)
        {
            using var content = new MultipartFormDataContent();
            using var streamContent = new StreamContent(imageStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(streamContent, "file", originalFileName);

            using var client = new HttpClient();
            var response = await client.PostAsync(_functionUrl, content);
            response.EnsureSuccessStatusCode();

            // Expecting JSON response { "Url": "<blob-url>" }
            var json = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            return json?["Url"] ?? throw new InvalidOperationException("Upload failed: no URL returned");
        }

    }
}
