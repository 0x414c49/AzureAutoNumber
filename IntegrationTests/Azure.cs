using System;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using NUnit.Framework;
using AutoNumber;
using System.Text;
using System.IO;
using AutoNumber.Interfaces;

namespace IntegrationTests.cs
{
    [TestFixture]
    public class Azure : Scenarios<Azure.TestScope>
    {
        readonly CloudStorageAccount storageAccount = CloudStorageAccount.DevelopmentStorageAccount;

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

        public sealed class TestScope : ITestScope
        {
            readonly CloudBlobClient blobClient;

            public TestScope(CloudStorageAccount account)
            {
                var ticks = DateTime.UtcNow.Ticks;
                IdScopeName = string.Format("AutoNumbertest{0}", ticks);
                ContainerName = string.Format("AutoNumbertest{0}", ticks);

                blobClient = account.CreateCloudBlobClient();
            }

            public string IdScopeName { get; private set; }
            public string ContainerName { get; private set; }

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
}
