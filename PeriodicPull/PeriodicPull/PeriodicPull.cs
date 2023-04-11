using System;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: FunctionsStartup(typeof(PeriodicPull.Startup))]

namespace PeriodicPull;

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        // Register HttpClient with the DI container            
        builder.Services.AddHttpClient("publicApis");
    }
}

public class PeriodicPull
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly TableClient _tableClient;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly BlobContainerClient _blobContainerClient;

    public PeriodicPull(IHttpClientFactory httpClientFactory,
        ILogger<PeriodicPull> logger)
    {
        _logger = logger;

        _httpClient = httpClientFactory.CreateClient("publicApis");
        _httpClient.BaseAddress = new Uri("https://api.publicapis.org/random");
        
        // Get configuration from environment variables
        var tableStorageConnectionString = Environment.GetEnvironmentVariable("TableStorageConnectionString");
        var tableStorageTableName = Environment.GetEnvironmentVariable("TableStorageTableName");
        var blobStorageConnectionString = Environment.GetEnvironmentVariable("BlobStorageConnectionString");
        var blobStorageContainerName = Environment.GetEnvironmentVariable("BlobStorageContainerName");
        
        _tableClient = new TableClient(tableStorageConnectionString,
            tableStorageTableName);

        _tableClient.CreateIfNotExists();

        _blobServiceClient = new BlobServiceClient(blobStorageConnectionString);
        _blobContainerClient = _blobServiceClient.GetBlobContainerClient(blobStorageContainerName);

        _blobContainerClient.CreateIfNotExists();
    }

    /// <summary>
    /// Every minute, pull data from a web API using a timer trigger
    /// </summary>
    /// <param name="myTimer">the timer</param>
    /// <returns>awaitable task</returns>
    [FunctionName("PeriodicPull")]
    public async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer)
    {
        // Send an HTTP GET request to the "random" endpoint
        using var response = await _httpClient.GetAsync("random?auth=null");

        _logger.LogInformation("Response status code: {StatusCode}", response.StatusCode);
        
        // Create a reasonable partition and row key for Table Storage
        var partitionKey = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var rowKey = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");

        var tableStorageEntity = new LogEntity(partitionKey,
            rowKey,
            response.StatusCode);

        // If the call didn't succeed, store the response in Azure Table Storage and return
        if (response.IsSuccessStatusCode == false)
        {
            await _tableClient.AddEntityAsync(tableStorageEntity);
            _logger.LogInformation("Successfully stored data to table storage");
            return;
        }

        // The call was successful, so let's store the response content in Azure Blob Storage
        // and link it to the log record in Azure Table Storage
        var blobId = Guid.NewGuid();
        tableStorageEntity.BlobId = blobId;

        // In parallel, store the response content in Azure Table Storage and Azure Blob Storage
        var tableStorageTask = _tableClient.AddEntityAsync(tableStorageEntity);

        var blobClient = _blobContainerClient.GetBlobClient(blobId.ToString());
        var blobStorageTask = blobClient.UploadAsync(await response.Content.ReadAsStreamAsync());

        await Task.WhenAll(tableStorageTask, blobStorageTask);

        _logger.LogInformation("Successfully stored data to table storage and blob storage");
    }
}