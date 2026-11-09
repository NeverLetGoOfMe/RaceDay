using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly IConfiguration _config;
    private BlobContainerClient? _container;

    public BlobStorageService(IConfiguration config)
    {
        _config = config;
    }

    // The container is created on first use, so the API still starts if storage is not configured
    private async Task<BlobContainerClient> GetContainerAsync()
    {
        if (_container != null)
        {
            return _container;
        }

        var connectionString = _config["AzureStorage:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Azure Blob Storage is not configured.");
        }

        var containerName = _config["AzureStorage:ContainerName"] ?? "raceday-images";
        var container = new BlobContainerClient(connectionString, containerName);
        await container.CreateIfNotExistsAsync();   // private container - images are served through the API

        _container = container;
        return container;
    }

    public async Task<string> UploadAsync(Stream content, string contentType, string extension)
    {
        var container = await GetContainerAsync();
        var blobName = $"{Guid.NewGuid():N}{extension}";

        await container.GetBlobClient(blobName).UploadAsync(content, new BlobHttpHeaders { ContentType = contentType });
        return blobName;
    }

    public async Task<(Stream Content, string ContentType)?> OpenReadAsync(string blobName)
    {
        var container = await GetContainerAsync();
        var blob = container.GetBlobClient(blobName);

        if (!(await blob.ExistsAsync()).Value)
        {
            return null;
        }

        var download = await blob.DownloadStreamingAsync();
        return (download.Value.Content, download.Value.Details.ContentType);
    }
}
