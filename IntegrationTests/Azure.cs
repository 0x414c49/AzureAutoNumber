using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AutoNumber;
using AutoNumber.Interfaces;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using NUnit.Framework;
using Testcontainers.Azurite;

namespace IntegrationTests.cs
{
    [TestFixture]
    public class Azure : Scenarios<TestScope>
    {
        private AzuriteContainer _azuriteContainer;
        private BlobServiceClient _blobServiceClient;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            _azuriteContainer = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:latest")
                .Build();
            
            await _azuriteContainer.StartAsync();
            
            var connectionString = _azuriteContainer.GetConnectionString();
            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            if (_azuriteContainer != null)
            {
                await _azuriteContainer.DisposeAsync();
            }
        }

        protected override TestScope BuildTestScope()
        {
            return new TestScope(_blobServiceClient);
        }

        protected override IOptimisticDataStore BuildStore(TestScope scope)
        {
            var blobOptimisticDataStore = new BlobOptimisticDataStore(_blobServiceClient, scope.ContainerName);
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