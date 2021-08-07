using AutoNumber;
using AutoNumber.Interfaces;
using Microsoft.WindowsAzure.Storage;

namespace IntegrationTests
{
    public class AzureTests : AbstractScenarioTests<TestScope>
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
}