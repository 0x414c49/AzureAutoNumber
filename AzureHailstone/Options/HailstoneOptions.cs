namespace AzureHailstone.Options
{
    public class HailstoneOptions
    {
        public int BatchSize { get; set; } = 100;
        public int MaxWriteAttempts { get; set; } = 100;
        public string StorageContainerName { get; set; } = "unique-urls";
    }
}
