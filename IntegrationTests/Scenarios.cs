using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoNumber;
using AutoNumber.Interfaces;
using NUnit.Framework;

namespace IntegrationTests.cs
{
    public abstract class Scenarios<TTestScope> where TTestScope : ITestScope
    {
        protected abstract IOptimisticDataStore BuildStore(TTestScope scope);
        protected abstract TTestScope BuildTestScope();

        [Test]
        public void ShouldReturnOneForFirstIdInNewScope()
        {
            // Arrange
            using (var testScope = BuildTestScope())
            {
                var store = BuildStore(testScope);
                var generator = new UniqueIdGenerator(store) {BatchSize = 3};

                // Act
                var generatedId = generator.NextId(testScope.IdScopeName);

                // Assert
                Assert.That(generatedId, Is.EqualTo(1));
            }
        }

        [Test]
        public void ShouldInitializeBlobForFirstIdInNewScope()
        {
            // Arrange
            using (var testScope = BuildTestScope())
            {
                var store = BuildStore(testScope);
                var generator = new UniqueIdGenerator(store) {BatchSize = 3};

                // Act
                generator.NextId(testScope.IdScopeName); //1

                // Assert
                Assert.That(testScope.ReadCurrentPersistedValue(), Is.EqualTo("4"));
            }
        }

        [Test]
        public void ShouldNotUpdateBlobAtEndOfBatch()
        {
            // Arrange
            using (var testScope = BuildTestScope())
            {
                var store = BuildStore(testScope);
                var generator = new UniqueIdGenerator(store) {BatchSize = 3};

                // Act
                generator.NextId(testScope.IdScopeName); //1
                generator.NextId(testScope.IdScopeName); //2
                generator.NextId(testScope.IdScopeName); //3

                // Assert
                Assert.That(testScope.ReadCurrentPersistedValue(), Is.EqualTo("4"));
            }
        }

        [Test]
        public void ShouldUpdateBlobWhenGeneratingNextIdAfterEndOfBatch()
        {
            // Arrange
            using (var testScope = BuildTestScope())
            {
                var store = BuildStore(testScope);
                var generator = new UniqueIdGenerator(store) {BatchSize = 3};

                // Act
                generator.NextId(testScope.IdScopeName); //1
                generator.NextId(testScope.IdScopeName); //2
                generator.NextId(testScope.IdScopeName); //3
                generator.NextId(testScope.IdScopeName); //4

                // Assert
                Assert.That(testScope.ReadCurrentPersistedValue(), Is.EqualTo("7"));
            }
        }

        [Test]
        public void ShouldReturnIdsFromThirdBatchIfSecondBatchTakenByAnotherGenerator()
        {
            // Arrange
            using (var testScope = BuildTestScope())
            {
                var store1 = BuildStore(testScope);
                var generator1 = new UniqueIdGenerator(store1) {BatchSize = 3};
                var store2 = BuildStore(testScope);
                var generator2 = new UniqueIdGenerator(store2) {BatchSize = 3};

                // Act
                generator1.NextId(testScope.IdScopeName); //1
                generator1.NextId(testScope.IdScopeName); //2
                generator1.NextId(testScope.IdScopeName); //3
                generator2.NextId(testScope.IdScopeName); //4
                var lastId = generator1.NextId(testScope.IdScopeName); //7

                // Assert
                Assert.That(lastId, Is.EqualTo(7));
            }
        }

        [Test]
        public void ShouldReturnIdsAcrossMultipleGenerators()
        {
            // Arrange
            using (var testScope = BuildTestScope())
            {
                var store1 = BuildStore(testScope);
                var generator1 = new UniqueIdGenerator(store1) {BatchSize = 3};
                var store2 = BuildStore(testScope);
                var generator2 = new UniqueIdGenerator(store2) {BatchSize = 3};

                // Act
                var generatedIds = new[]
                {
                    generator1.NextId(testScope.IdScopeName), //1
                    generator1.NextId(testScope.IdScopeName), //2
                    generator1.NextId(testScope.IdScopeName), //3
                    generator2.NextId(testScope.IdScopeName), //4
                    generator1.NextId(testScope.IdScopeName), //7
                    generator2.NextId(testScope.IdScopeName), //5
                    generator2.NextId(testScope.IdScopeName), //6
                    generator2.NextId(testScope.IdScopeName), //10
                    generator1.NextId(testScope.IdScopeName), //8
                    generator1.NextId(testScope.IdScopeName) //9
                };

                // Assert
                Assert.That(generatedIds, Is.EqualTo(new[] {1, 2, 3, 4, 7, 5, 6, 10, 8, 9}));
            }
        }

        [Test]
        public void ShouldSupportUsingOneGeneratorFromMultipleThreads()
        {
            // Arrange
            using (var testScope = BuildTestScope())
            {
                var store = BuildStore(testScope);
                var generator = new UniqueIdGenerator(store) {BatchSize = 1000};
                const int testLength = 10000;

                // Act
                var generatedIds = new ConcurrentQueue<long>();
                var threadIds = new ConcurrentQueue<int>();
                var scopeName = testScope.IdScopeName;
                Parallel.For(
                    0,
                    testLength,
                    new ParallelOptions {MaxDegreeOfParallelism = 10},
                    i =>
                    {
                        generatedIds.Enqueue(generator.NextId(scopeName));
                        threadIds.Enqueue(Thread.CurrentThread.ManagedThreadId);
                    });

                // Assert we generated the right count of ids
                Assert.That(generatedIds.Count, Is.EqualTo(testLength));

                // Assert there were no duplicates
                Assert.That(generatedIds.GroupBy(n => n).Any(g => g.Count() != 1), Is.False);

                // Assert we used multiple threads
                var uniqueThreadsUsed = threadIds.Distinct().Count();
                if (uniqueThreadsUsed == 1)
                    Assert.Inconclusive("The test failed to actually utilize multiple threads");
            }
        }
    }
}