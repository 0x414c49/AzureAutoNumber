using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoNumber;
using AutoNumber.Interfaces;
using IntegrationTests.Interfaces;
using Xunit;

namespace IntegrationTests
{
    public abstract class AbstractScenarioTests<TTestScope> where TTestScope : ITestScope
    {
        protected abstract IOptimisticDataStore BuildStore(TTestScope scope);
        protected abstract TTestScope BuildTestScope();

        [Fact]
        public void Should_Return_One_For_First_Id_In_NewScope()
        {
            // Arrange
            using var testScope = BuildTestScope();
            var store = BuildStore(testScope);
            var generator = new UniqueIdGenerator(store) { BatchSize = 3 };

            // Act
            var generatedId = generator.NextId(testScope.IdScopeName);

            // Assert
            Assert.Equal(1, generatedId);
        }

        [Fact]
        public void Should_Initialize_Blob_For_First_Id_In_NewScope()
        {
            // Arrange
            using var testScope = BuildTestScope();
            var store = BuildStore(testScope);
            var generator = new UniqueIdGenerator(store) { BatchSize = 3 };

            // Act
            generator.NextId(testScope.IdScopeName); //1

            // Assert
            Assert.Equal("4", testScope.ReadCurrentPersistedValue());
        }

        [Fact]
        public void Should_Not_Update_Blob_At_End_Of_Batch()
        {
            // Arrange
            using var testScope = BuildTestScope();
            var store = BuildStore(testScope);
            var generator = new UniqueIdGenerator(store) { BatchSize = 3 };

            // Act
            generator.NextId(testScope.IdScopeName); //1
            generator.NextId(testScope.IdScopeName); //2
            generator.NextId(testScope.IdScopeName); //3

            // Assert
            Assert.Equal("4", testScope.ReadCurrentPersistedValue());
        }

        [Fact]
        public void Should_Update_Blob_When_Generating_NextId_After_End_Of_Batch()
        {
            // Arrange
            using var testScope = BuildTestScope();
            var store = BuildStore(testScope);
            var generator = new UniqueIdGenerator(store) { BatchSize = 3 };

            // Act
            generator.NextId(testScope.IdScopeName); //1
            generator.NextId(testScope.IdScopeName); //2
            generator.NextId(testScope.IdScopeName); //3
            generator.NextId(testScope.IdScopeName); //4

            // Assert
            Assert.Equal("7", testScope.ReadCurrentPersistedValue());
        }

        [Fact]
        public void Should_Return_Ids_From_Third_Batch_If_Second_Batch_Taken_By_Another_Generator()
        {
            // Arrange
            using var testScope = BuildTestScope();
            var store1 = BuildStore(testScope);
            var generator1 = new UniqueIdGenerator(store1) { BatchSize = 3 };
            var store2 = BuildStore(testScope);
            var generator2 = new UniqueIdGenerator(store2) { BatchSize = 3 };

            // Act
            generator1.NextId(testScope.IdScopeName); //1
            generator1.NextId(testScope.IdScopeName); //2
            generator1.NextId(testScope.IdScopeName); //3
            generator2.NextId(testScope.IdScopeName); //4
            var lastId = generator1.NextId(testScope.IdScopeName); //7

            // Assert
            Assert.Equal(7, lastId);
        }

        [Fact]
        public void Should_Return_Ids_Across_Multiple_Generators()
        {
            // Arrange
            using var testScope = BuildTestScope();
            var store1 = BuildStore(testScope);
            var generator1 = new UniqueIdGenerator(store1) { BatchSize = 3 };
            var store2 = BuildStore(testScope);
            var generator2 = new UniqueIdGenerator(store2) { BatchSize = 3 };

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
            var expected = new long[] { 1, 2, 3, 4, 7, 5, 6, 10, 8, 9 };
            Assert.Equal(expected, generatedIds);
        }

        [Fact]
        public void Should_Support_Using_OneGenerator_From_Multiple_Threads()
        {
            // Arrange
            using var testScope = BuildTestScope();
            var store = BuildStore(testScope);
            var generator = new UniqueIdGenerator(store) { BatchSize = 1000 };
            const int testLength = 10000;

            // Act
            var generatedIds = new ConcurrentQueue<long>();
            var threadIds = new ConcurrentQueue<int>();
            var scopeName = testScope.IdScopeName;
            Parallel.For(
                0,
                testLength,
                new ParallelOptions { MaxDegreeOfParallelism = 10 },
                i =>
                {
                    generatedIds.Enqueue(generator.NextId(scopeName));
                    threadIds.Enqueue(Thread.CurrentThread.ManagedThreadId);
                });

            // Assert we generated the right count of ids
            Assert.Equal(testLength, generatedIds.Count);

            // Assert there were no duplicates
            Assert.DoesNotContain(generatedIds.GroupBy(n => n), g => g.Count() != 1);

            // Assert we used multiple threads
            var uniqueThreadsUsed = threadIds.Distinct().Count();
            if (uniqueThreadsUsed == 1)
                Assert.False(true ,"The test failed to actually utilize multiple threads");
        }
    }
}