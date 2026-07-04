namespace CsvIngestionApi.Services;

/// <summary>
/// Abstraction over Azure Blob Storage uploads so controllers stay free of SDK details
/// and the implementation can be mocked in tests.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Uploads a stream to blob storage and returns the absolute URI of the created blob.
    /// </summary>
    /// <param name="content">The file content stream to upload.</param>
    /// <param name="fileName">The original file name; used to derive a unique blob name.</param>
    /// <param name="contentType">The MIME content type to record on the blob.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The absolute <see cref="Uri"/> of the uploaded blob.</returns>
    Task<Uri> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);
}
