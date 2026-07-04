using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Storage.Blobs;
using CsvIngestionApi.Configuration;
using CsvIngestionApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

// Load secrets (such as the Storage connection string) from Azure Key Vault.
// Locally this authenticates with a service principal (client ID + secret); this
// credential is planned to be replaced by Managed Identity in a later step.
var keyVaultUri = builder.Configuration["KeyVault:Uri"];
if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    var credential = new ClientSecretCredential(
        builder.Configuration["KeyVault:TenantId"],
        builder.Configuration["KeyVault:ClientId"],
        builder.Configuration["KeyVault:ClientSecret"]);

    // Key Vault secrets named "Storage--ConnectionString" map to config key
    // "Storage:ConnectionString", so StorageOptions binds automatically.
    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), credential);
}

// Add services to the container.
builder.Services.AddControllers();

// Secure the API with Microsoft Entra ID (JWT bearer).
// Inbound tokens issued by the configured tenant/app registration are validated here.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

// Enable authorization. The API requires a valid Entra ID token (see [Authorize]).
builder.Services.AddAuthorization();

// Bind and validate Azure Blob Storage configuration.
builder.Services
    .AddOptions<StorageOptions>()
    .Bind(builder.Configuration.GetSection(StorageOptions.SectionName))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.ConnectionString),
        "Storage:ConnectionString must be configured.");

// Register a singleton BlobServiceClient using the connection string sourced from Key Vault.
builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<IOptions<StorageOptions>>().Value;
    return new BlobServiceClient(options.ConnectionString);
});

builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
