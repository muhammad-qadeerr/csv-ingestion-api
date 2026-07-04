using CsvIngestionApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CsvIngestionApi.Controllers;

/// <summary>
/// Endpoint for ingesting CSV files and persisting them to Azure Blob Storage.
/// </summary>
[ApiController]
[Authorize]
[Route("api/csv")]
public sealed class CsvUploadController : ControllerBase
{
    private const string CsvContentType = "text/csv";
    private static readonly string[] AllowedContentTypes =
    {
        "text/csv",
        "application/csv",
        "application/vnd.ms-excel",
        "text/plain"
    };

    private readonly IBlobStorageService _blobStorageService;
    private readonly ILogger<CsvUploadController> _logger;

    public CsvUploadController(
        IBlobStorageService blobStorageService,
        ILogger<CsvUploadController> logger)
    {
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    /// <summary>
    /// Uploads a single CSV file and stores it in blob storage.
    /// </summary>
    /// <param name="file">The CSV file sent as multipart/form-data.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The location of the stored blob.</returns>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadCsvToBlob(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("A non-empty CSV file is required.");
        }

        if (!IsCsvFile(file))
        {
            return BadRequest("Only CSV files (.csv) are accepted.");
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var blobUri = await _blobStorageService.UploadAsync(
                stream,
                file.FileName,
                CsvContentType,
                cancellationToken);

            return Created(blobUri, new { fileName = file.FileName, blobUri });
        }
        catch (Exception ex)
        {
            // Surface a clean error to the caller while logging the detail server-side.
            _logger.LogError(ex, "Failed to upload CSV file {FileName}.", file.FileName);
            return Problem(
                title: "CSV upload failed.",
                detail: "The file could not be stored. Please try again later.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static bool IsCsvFile(IFormFile file)
    {
        return Path.GetExtension(file.FileName)
            .Equals(".csv", StringComparison.OrdinalIgnoreCase);
    }
}
