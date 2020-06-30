using System;
using NUnit.Framework;
using AutoNumber;
using System.Text;
using System.IO;
using AutoNumber.Interfaces;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage;

namespace IntegrationTests.cs
{
    [TestFixture]
    public class Azure : Scenarios<TestScope>
    {
        private readonly CloudStorageAccount storageAccount = CloudStorageAccount.DevelopmentStorageAccount;

        protected override TestScope BuildTestScope()
        {
            return new TestScope(CloudStorageAccount.DevelopmentStorageAccount);
        }

        protected override IOptimisticDataStore BuildStore(TestScope scope)
        {
            var blobOptimisticDataStore = new BlobOptimisticDataStore(storageAccount, scope.ContainerName);
            blobOptimisticDataStore.Init().GetAwaiter().GetResult();
            return blobOptimisticDataStore;
        }
    }

    public sealed class TestScope : ITestScope
    {
        private readonly CloudBlobClient blobClient;

        public TestScope(CloudStorageAccount account)
        {
            var ticks = DateTime.UtcNow.Ticks;
            IdScopeName = string.Format("autonumbertest{0}", ticks);
            ContainerName = string.Format("autonumbertest{0}", ticks);

            blobClient = account.CreateCloudBlobClient();
        }

        public string IdScopeName { get; }
        public string ContainerName { get; }

        public string ReadCurrentPersistedValue()
        {
            var blobContainer = blobClient.GetContainerReference(ContainerName);
            var blob = blobContainer.GetBlockBlobReference(IdScopeName);
            using (var stream = new MemoryStream())
            {
                blob.DownloadToStreamAsync(stream).GetAwaiter().GetResult();
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public void Dispose()
        {
            var blobContainer = blobClient.GetContainerReference(ContainerName);
            blobContainer.DeleteAsync().GetAwaiter().GetResult();
        }
    }
}
