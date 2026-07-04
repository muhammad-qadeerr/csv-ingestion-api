namespace CsvIngestionApi.Configuration;

/// <summary>
/// Strongly typed configuration for the Azure Blob Storage destination.
/// Bound from the "Storage" section of configuration.
/// </summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>
    /// The Storage account connection string, configured in appsettings.json.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// The container that uploaded CSV files are written to.
    /// </summary>
    public string ContainerName { get; set; } = "csv-uploads";
}
