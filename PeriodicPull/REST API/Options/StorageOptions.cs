namespace RestApi.Options;

public class StorageOptions
{
    public string? TableStorageConnectionString { get; set; }
    public string? TableStorageTableName { get; set; }
    public string? BlobStorageConnectionString { get; set; }
    public string? BlobStorageContainerName { get; set; }
}