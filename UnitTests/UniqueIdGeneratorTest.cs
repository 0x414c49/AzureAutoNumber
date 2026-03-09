using System;
using AutoNumber.Exceptions;
using AutoNumber.Interfaces;
using NSubstitute;
using Xunit;

namespace AutoNumber.UnitTests
{
    public class UniqueIdGeneratorTest
    {
        [Fact]
        public void ConstructorShouldNotRetrieveDataFromStore()
        {
            var store = Substitute.For<IOptimisticDataStore>();
            new UniqueIdGenerator(store);
            store.DidNotReceiveWithAnyArgs().GetData(null);
        }

        [Fact]
        public void MaxWriteAttemptsShouldThrowArgumentOutOfRangeExceptionWhenValueIsNegative()
        {
            var store = Substitute.For<IOptimisticDataStore>();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new UniqueIdGenerator(store)
                {
                    MaxWriteAttempts = -1
                });
        }

        [Fact]
        public void MaxWriteAttemptsShouldThrowArgumentOutOfRangeExceptionWhenValueIsZero()
        {
            var store = Substitute.For<IOptimisticDataStore>();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new UniqueIdGenerator(store)
                {
                    MaxWriteAttempts = 0
                });
        }

        [Fact]
        public void NextIdShouldReturnNumbersSequentially()
        {
            var store = Substitute.For<IOptimisticDataStore>();
            store.GetData("test").Returns("0", "250");
            store.TryOptimisticWrite("test", "3").Returns(true);

            var subject = new UniqueIdGenerator(store)
            {
                BatchSize = 3
            };

            Assert.Equal(0, subject.NextId("test"));
            Assert.Equal(1, subject.NextId("test"));
            Assert.Equal(2, subject.NextId("test"));
        }

        [Fact]
        public void NextIdShouldRollOverToNewBlockWhenCurrentBlockIsExhausted()
        {
            var store = Substitute.For<IOptimisticDataStore>();
            store.GetData("test").Returns("0", "250");
            store.TryOptimisticWrite("test", "3").Returns(true);
            store.TryOptimisticWrite("test", "253").Returns(true);

            var subject = new UniqueIdGenerator(store)
            {
                BatchSize = 3
            };

            Assert.Equal(0, subject.NextId("test"));
            Assert.Equal(1, subject.NextId("test"));
            Assert.Equal(2, subject.NextId("test"));
            Assert.Equal(250, subject.NextId("test"));
            Assert.Equal(251, subject.NextId("test"));
            Assert.Equal(252, subject.NextId("test"));
        }

        [Fact]
        public void NextIdShouldThrowExceptionOnCorruptData()
        {
            var store = Substitute.For<IOptimisticDataStore>();
            store.GetData("test").Returns("abc");

            Assert.Throws<UniqueIdGenerationException>(() =>
            {
                var generator = new UniqueIdGenerator(store);
                generator.NextId("test");
            });
        }

        [Fact]
        public void NextIdShouldThrowExceptionOnNullData()
        {
            var store = Substitute.For<IOptimisticDataStore>();
            store.GetData("test").Returns((string)null);

            Assert.Throws<UniqueIdGenerationException>(() =>
            {
                var generator = new UniqueIdGenerator(store);
                generator.NextId("test");
            });
        }

        [Fact]
        public void NextIdShouldThrowExceptionWhenRetriesAreExhausted()
        {
            var store = Substitute.For<IOptimisticDataStore>();
            store.GetData("test").Returns("0");
            store.TryOptimisticWrite("test", "3").Returns(false, false, false, true);

            var generator = new UniqueIdGenerator(store)
            {
                MaxWriteAttempts = 3
            };

            var exception = Assert.Throws<UniqueIdGenerationException>(() => generator.NextId("test"));
            Assert.StartsWith("Failed to update the data store after 3 attempts.", exception.Message);
        }
    }
}
