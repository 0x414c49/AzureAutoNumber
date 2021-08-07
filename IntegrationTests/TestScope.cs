using System;
using System.IO;
using System.Text;
using IntegrationTests.Interfaces;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace IntegrationTests
{
    public sealed class TestScope : ITestScope
    {
        private const string Name = "autonumbertest";
        private readonly CloudBlobClient blobClient;

        public TestScope(CloudStorageAccount account)
        {
            var ticks = DateTime.UtcNow.Ticks;
            IdScopeName = $"{Name}{ticks}";
            ContainerName = $"{Name}{ticks}";

            blobClient = account.CreateCloudBlobClient();
        }

        public string ContainerName { get; }

        public string IdScopeName { get; }

        public string ReadCurrentPersistedValue()
        {
            var blobContainer = blobClient.GetContainerReference(ContainerName);
            var blob = blobContainer.GetBlockBlobReference(IdScopeName);
            using var stream = new MemoryStream();
            blob.DownloadToStreamAsync(stream).GetAwaiter().GetResult();
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        public void Dispose()
        {
            var blobContainer = blobClient.GetContainerReference(ContainerName);
            blobContainer.DeleteAsync().GetAwaiter().GetResult();
        }
    }
}