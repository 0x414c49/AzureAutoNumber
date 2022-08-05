using Azure.Storage.Blobs;

namespace AutoNumber.Options
{
    public class AutoNumberOptions
    {
        public int BatchSize { get; set; } = 100;
        public int MaxWriteAttempts { get; set; } = 100;
        public string StorageContainerName { get; set; } = "unique-urls";
        public string StorageAccountConnectionString { get; set; }
        public BlobServiceClient BlobServiceClient { get; set; }
    }
}