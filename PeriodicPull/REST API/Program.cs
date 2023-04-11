using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using RestApi.Options;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using RestApi.Models;
using Microsoft.Extensions.Azure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add options to the container (not strictly necessary for this example)
builder.Services.AddOptions<StorageOptions>()
    .Configure<IConfiguration>((options, configuration) =>
    {
        configuration.GetSection("StorageOptions").Bind(options);
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Get options for the storage clients
StorageOptions storageOptions = new();
app.Configuration.GetSection("StorageOptions").Bind(storageOptions);

// Setup the Table Storage Client
var tableClient = new TableClient(storageOptions.TableStorageConnectionString,
    storageOptions.TableStorageTableName);

tableClient.CreateIfNotExists();

// Setup the Blob Storage Client

var blobServiceClient = new BlobServiceClient(storageOptions.BlobStorageConnectionString);
var blobContainerClient = blobServiceClient.GetBlobContainerClient(storageOptions.BlobStorageContainerName);

// This implements fetching log-entries, including paging, optional max entries
// and the option of using an exposed continuation token. Paging is not exact, it doesn't
// discard superfluous messages, but it stops reading from table storage when threshold is
// hit
app.MapGet("/logentries", async ([FromQuery][Required] DateTime from,
    [FromQuery][Required] DateTime to,
    [FromQuery] string? continuationToken,
    [FromQuery] int? maxEntries) =>
{
    string fromPartitionKey = from.ToString("yyyy-MM-dd");
    string toPartitionKey = to.ToString("yyyy-MM-dd");
    string fromRowKey = from.ToString("yyyy-MM-dd HH:mm:ss.fff");
    string toRowKey = to.ToString("yyyy-MM-dd HH:mm:ss.fff");

    var pageableQuery = tableClient.QueryAsync<PeriodicPull.LogEntity>(le =>
        string.Compare(le.PartitionKey, fromPartitionKey) >= 0
        && string.Compare(le.PartitionKey, toPartitionKey) <= 0
        && string.Compare(le.RowKey, fromRowKey) >= 0
        && string.Compare(le.RowKey, toRowKey) <= 0);

    LogEntitiesResponse response = new();

    await foreach (var entry in pageableQuery.AsPages(continuationToken))
    {
        response.LogEntities.AddRange(entry.Values.Select(le => new LogEntity
        {
            BlobId = le.BlobId,
            Date = le.Timestamp,
            HttpStatusCode = le.HttpStatusCode,
            IsSuccessStatusCode = (int)le.HttpStatusCode >= 200 && (int)le.HttpStatusCode <= 299
        }).ToList());

        response.ContinuationToken = entry.ContinuationToken;

        if (maxEntries != 0
            && response.LogEntities.Count >= maxEntries) break;
    }

    return response;
})
.WithName("GetLogEntries");

// Simple fix, should be from route
app.MapGet("/payloads", async ([FromQuery] Guid blobId) =>
{
    // Grab the blob from Blob Storage
    var blobClient = blobContainerClient.GetBlobClient(blobId.ToString());
    if (await blobClient.ExistsAsync())
    {
        var data = await blobClient.OpenReadAsync();

        // Download the file details async
        using var ms = new MemoryStream();
        var content = await blobClient.DownloadToAsync(ms);

        var blobContent = System.Text.Encoding.UTF8.GetString(ms.ToArray());

        // Create new BlobDto with blob data from variables
        return Results.Content(blobContent, contentType: "application/json");
    }
    else
    {
        return Results.NotFound("Blob not found");
    }
}).WithName("GetPayload");

app.Run();