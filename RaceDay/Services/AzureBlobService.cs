using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Services
{
    public class AzureBlobService
    {
        private readonly BlobContainerClient? _containerClient;
        private readonly IWebHostEnvironment _env;

        public AzureBlobService(IConfiguration configuration, IWebHostEnvironment env)
        {
            _env = env;
            var connectionString = configuration["AzureBlobStorage:ConnectionString"];
            var containerName = configuration["AzureBlobStorage:ContainerName"] ?? "raceday-media";

            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                _containerClient = new BlobContainerClient(connectionString, containerName);
                _containerClient.CreateIfNotExists(PublicAccessType.Blob);
            }
        }

        public async Task<string> UploadAsync(IFormFile file, string folder)
        {
            var extension = Path.GetExtension(file.FileName);
            var safeFolder = folder.Trim('/');
            var blobName = $"{safeFolder}/{Guid.NewGuid()}{extension}";

            if (_containerClient != null)
            {
                var blobClient = _containerClient.GetBlobClient(blobName);
                await using var stream = file.OpenReadStream();
                await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });
                return blobClient.Uri.ToString();
            }

            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var targetDir = Path.Combine(webRoot, "uploads", safeFolder.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(targetDir);

            var localFileName = $"{Guid.NewGuid()}{extension}";
            var fullPath = Path.Combine(targetDir, localFileName);

            await using var fileStream = File.Create(fullPath);
            await file.CopyToAsync(fileStream);

            return $"/uploads/{safeFolder}/{localFileName}";
        }
    }
}