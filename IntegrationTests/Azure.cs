using System;
using System.IO;
using System.Text;
using AutoNumber;
using AutoNumber.Interfaces;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using NUnit.Framework;

namespace IntegrationTests.cs
{
    [TestFixture]
    public class Azure : Scenarios<TestScope>
    {
        private readonly BlobServiceClient blobServiceClient = new BlobServiceClient("UseDevelopmentStorage=true");

        protected override TestScope BuildTestScope()
        {
            return new TestScope(new BlobServiceClient("UseDevelopmentStorage=true"));
        }

        protected override IOptimisticDataStore BuildStore(TestScope scope)
        {
            var blobOptimisticDataStore = new BlobOptimisticDataStore(blobServiceClient, scope.ContainerName);
            blobOptimisticDataStore.Init();
            return blobOptimisticDataStore;
        }
    }

    public sealed class TestScope : ITestScope
    {
        private readonly BlobServiceClient blobServiceClient;

        public TestScope(BlobServiceClient blobServiceClient)
        {
            var ticks = DateTime.UtcNow.Ticks;
            IdScopeName = string.Format("autonumbertest{0}", ticks);
            ContainerName = string.Format("autonumbertest{0}", ticks);

            this.blobServiceClient = blobServiceClient;
        }

        public string ContainerName { get; }

        public string IdScopeName { get; }

        public string ReadCurrentPersistedValue()
        {
            var blobContainer = blobServiceClient.GetBlobContainerClient(ContainerName);
            var blob = blobContainer.GetBlockBlobClient(IdScopeName);
            using (var stream = new MemoryStream())
            {
                blob.DownloadToAsync(stream).GetAwaiter().GetResult();
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public void Dispose()
        {
            var blobContainer = blobServiceClient.GetBlobContainerClient(ContainerName);
            blobContainer.DeleteAsync().GetAwaiter().GetResult();
        }
    }
}