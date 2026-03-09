using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoNumber;
using AutoNumber.Interfaces;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Testcontainers.Azurite;
using Xunit;

namespace IntegrationTests.cs
{
    public interface ITestScope : IAsyncDisposable
    {
        string IdScopeName { get; }
        string ReadCurrentPersistedValue();
    }

    [Collection("Azurite")]
    public class AzureFixture : IAsyncLifetime
    {
        private readonly AzuriteContainer _azuriteContainer;
        public BlobServiceClient BlobServiceClient { get; private set; }

        public AzureFixture()
        {
            _azuriteContainer = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:latest")
                .WithCommand("--skipApiVersionCheck")
                .Build();
        }

        public async Task InitializeAsync()
        {
            await _azuriteContainer.StartAsync();
            var connectionString = _azuriteContainer.GetConnectionString();
            BlobServiceClient = new BlobServiceClient(connectionString);
        }

        public async Task DisposeAsync()
        {
            await _azuriteContainer.DisposeAsync();
        }
    }

    [Collection("Azurite")]
    public class AzureTests : IClassFixture<AzureFixture>
    {
        private readonly AzureFixture _fixture;
        private const int DefaultBatchSize = 3;
        private const int LargeBatchSize = 1000;
        private const int ThreadCount = 10;
        private const int TestLength = 10000;
        private const int MinUniqueThreads = 1;

        public AzureTests(AzureFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void ShouldReturnOneForFirstIdInNewScope()
        {
            using var testScope = BuildTestScope();
            var store = BuildStore(testScope);
            var generator = new UniqueIdGenerator(store) { BatchSize = DefaultBatchSize };

            var generatedId = generator.NextId(testScope.IdScopeName);

            Assert.Equal(1, generatedId);
        }

        [Fact]
        public void ShouldInitializeBlobForFirstIdInNewScope()
        {
            using var testScope = BuildTestScope();
            var store = BuildStore(testScope);
            var generator = new UniqueIdGenerator(store) { BatchSize = DefaultBatchSize };

            generator.NextId(testScope.IdScopeName);

            Assert.Equal("4", testScope.ReadCurrentPersistedValue());
        }

        [Fact]
        public void ShouldNotUpdateBlobAtEndOfBatch()
        {
            using var testScope = BuildTestScope();
            var store = BuildStore(testScope);
            var generator = new UniqueIdGenerator(store) { BatchSize = DefaultBatchSize };

            generator.NextId(testScope.IdScopeName);
            generator.NextId(testScope.IdScopeName);
            generator.NextId(testScope.IdScopeName);

            Assert.Equal("4", testScope.ReadCurrentPersistedValue());
        }

        [Fact]
        public void ShouldUpdateBlobWhenGeneratingNextIdAfterEndOfBatch()
        {
            using var testScope = BuildTestScope();
            var store = BuildStore(testScope);
            var generator = new UniqueIdGenerator(store) { BatchSize = DefaultBatchSize };

            generator.NextId(testScope.IdScopeName);
            generator.NextId(testScope.IdScopeName);
            generator.NextId(testScope.IdScopeName);
            generator.NextId(testScope.IdScopeName);

            Assert.Equal("7", testScope.ReadCurrentPersistedValue());
        }

        [Fact]
        public void ShouldReturnIdsFromThirdBatchIfSecondBatchTakenByAnotherGenerator()
        {
            using var testScope = BuildTestScope();
            var store1 = BuildStore(testScope);
            var generator1 = new UniqueIdGenerator(store1) { BatchSize = DefaultBatchSize };
            var store2 = BuildStore(testScope);
            var generator2 = new UniqueIdGenerator(store2) { BatchSize = DefaultBatchSize };

            generator1.NextId(testScope.IdScopeName);
            generator1.NextId(testScope.IdScopeName);
            generator1.NextId(testScope.IdScopeName);
            generator2.NextId(testScope.IdScopeName);
            var lastId = generator1.NextId(testScope.IdScopeName);

            Assert.Equal(7, lastId);
        }

        [Fact]
        public void ShouldReturnIdsAcrossMultipleGenerators()
        {
            using var testScope = BuildTestScope();
            var store1 = BuildStore(testScope);
            var generator1 = new UniqueIdGenerator(store1) { BatchSize = DefaultBatchSize };
            var store2 = BuildStore(testScope);
            var generator2 = new UniqueIdGenerator(store2) { BatchSize = DefaultBatchSize };

            var generatedIds = new[]
            {
                generator1.NextId(testScope.IdScopeName),
                generator1.NextId(testScope.IdScopeName),
                generator1.NextId(testScope.IdScopeName),
                generator2.NextId(testScope.IdScopeName),
                generator1.NextId(testScope.IdScopeName),
                generator2.NextId(testScope.IdScopeName),
                generator2.NextId(testScope.IdScopeName),
                generator2.NextId(testScope.IdScopeName),
                generator1.NextId(testScope.IdScopeName),
                generator1.NextId(testScope.IdScopeName)
            };

            var expected = new long[] { 1, 2, 3, 4, 7, 5, 6, 10, 8, 9 };
            Assert.Equal(expected, generatedIds);
        }

        [Fact]
        public void ShouldSupportUsingOneGeneratorFromMultipleThreads()
        {
            using var testScope = BuildTestScope();
            var store = BuildStore(testScope);
            var generator = new UniqueIdGenerator(store) { BatchSize = LargeBatchSize };

            var generatedIds = new ConcurrentQueue<long>();
            var threadIds = new ConcurrentQueue<int>();
            var scopeName = testScope.IdScopeName;
            Parallel.For(
                0,
                TestLength,
                new ParallelOptions { MaxDegreeOfParallelism = ThreadCount },
                i =>
                {
                    generatedIds.Enqueue(generator.NextId(scopeName));
                    threadIds.Enqueue(Thread.CurrentThread.ManagedThreadId);
                });

            Assert.Equal(TestLength, generatedIds.Count);
            Assert.DoesNotContain(generatedIds.GroupBy(n => n), g => g.Count() != MinUniqueThreads);

            var uniqueThreadsUsed = threadIds.Distinct().Count();
            if (uniqueThreadsUsed == MinUniqueThreads)
            {
                return;
            }
        }

        private TestScope BuildTestScope()
        {
            return new TestScope(_fixture.BlobServiceClient);
        }

        private IOptimisticDataStore BuildStore(TestScope scope)
        {
            var blobOptimisticDataStore = new BlobOptimisticDataStore(_fixture.BlobServiceClient, scope.ContainerName);
            blobOptimisticDataStore.Init();
            return blobOptimisticDataStore;
        }
    }

    public sealed class TestScope : ITestScope, IDisposable
    {
        private readonly BlobServiceClient blobServiceClient;
        private readonly BlobContainerClient containerClient;

        public TestScope(BlobServiceClient blobServiceClient)
        {
            var ticks = DateTime.UtcNow.Ticks;
            IdScopeName = $"autonumbertest{ticks}";
            ContainerName = $"autonumbertest{ticks}";

            this.blobServiceClient = blobServiceClient;
            containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
        }

        public string ContainerName { get; }

        public string IdScopeName { get; }

        public string ReadCurrentPersistedValue()
        {
            var blob = containerClient.GetBlockBlobClient(IdScopeName);
            using var stream = new MemoryStream();
            blob.DownloadToAsync(stream).GetAwaiter().GetResult();
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        public void Dispose()
        {
            containerClient.DeleteAsync().GetAwaiter().GetResult();
        }

        public async ValueTask DisposeAsync()
        {
            await containerClient.DeleteAsync();
        }
    }

    [CollectionDefinition("Azurite", DisableParallelization = true)]
    public class AzuriteCollection { }
}
