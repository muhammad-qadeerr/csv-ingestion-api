using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CsvIngestionApi.Configuration;
using Microsoft.Extensions.Options;

namespace CsvIngestionApi.Services;

/// <summary>
/// Uploads CSV files to Azure Blob Storage.
/// Authentication is handled by the injected <see cref="BlobServiceClient"/>, which is
/// configured with DefaultAzureCredential (Managed Identity in Azure, developer
/// credentials locally) — no account keys or connection strings are used.
/// </summary>
public sealed class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly StorageOptions _options;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(
        BlobServiceClient blobServiceClient,
        IOptions<StorageOptions> options,
        ILogger<BlobStorageService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Uri> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_options.ContainerName);

        // Ensure the destination container exists. The blob SDK applies retry policies
        // with exponential backoff for transient failures automatically.
        await containerClient.CreateIfNotExistsAsync(
            PublicAccessType.None,
            cancellationToken: cancellationToken);

        // Derive a unique, time-ordered blob name so concurrent uploads of the same
        // file name do not overwrite each other.
        var blobName = BuildBlobName(fileName);
        var blobClient = containerClient.GetBlobClient(blobName);

        _logger.LogInformation(
            "Uploading CSV file {FileName} to blob {BlobName} in container {Container}.",
            fileName, blobName, _options.ContainerName);

        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        };

        await blobClient.UploadAsync(content, uploadOptions, cancellationToken);

        _logger.LogInformation("Successfully uploaded blob {BlobUri}.", blobClient.Uri);

        return blobClient.Uri;
    }

    private static string BuildBlobName(string fileName)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'");
        var safeName = Path.GetFileName(fileName);
        return $"{timestamp}-{Guid.NewGuid():N}-{safeName}";
    }
}
