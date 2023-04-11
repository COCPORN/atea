using System;
using System.Net;
using Azure;
using Azure.Data.Tables;

namespace PeriodicPull;

public class LogEntity : ITableEntity
{
    public LogEntity(string partitionKey, 
        string rowKey,
        HttpStatusCode httpStatusCode)
    {
        PartitionKey = partitionKey;
        RowKey = rowKey;
        HttpStatusCode = httpStatusCode;
    }
    
    public LogEntity() { }
    
    public string PartitionKey { get; set; }

    public string RowKey { get; set; }
    
    public DateTimeOffset? Timestamp { get; set; }
    
    public ETag ETag { get; set; }
    
    /// <summary>
    /// If the call was not successful over HTTP, this will be Guid.Empty
    /// </summary>
    public Guid BlobId { get; set; }
    
    public HttpStatusCode HttpStatusCode { get; set; }
}