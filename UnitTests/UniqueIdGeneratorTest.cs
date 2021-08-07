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
            // ReSharper disable once ObjectCreationAsStatement
            new UniqueIdGenerator(store);
            store.DidNotReceiveWithAnyArgs().GetData(null);
        }

        [Xunit.Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void MaxWriteAttempts_Should_Throw_ArgumentOutOfRangeException_When_Value_Is_Equal_Or_Lower_Than_Zero
            (int maxWriteAttempts)
        {
            var store = Substitute.For<IOptimisticDataStore>();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                // ReSharper disable once ObjectCreationAsStatement
                new UniqueIdGenerator(store)
                {
                    MaxWriteAttempts = maxWriteAttempts
                };
            });
        }

        [Fact]
        public void NextId_Should_Return_Numbers_Sequentially()
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
        public void NextId_Should_RollOver_To_New_Block_When_Current_Block_Is_Exhausted()
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
        public void NextId_Should_Throw_Exception_On_CorruptData()
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
        public void NextId_Should_Throw_Exception_On_NullData()
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
        public void NextId_Should_Throw_Exception_When_Retries_Are_Exhausted()
        {
            var store = Substitute.For<IOptimisticDataStore>();
            store.GetData("test").Returns("0");
            store.TryOptimisticWrite("test", "3").Returns(false, false, false, true);

            var generator = new UniqueIdGenerator(store)
            {
                MaxWriteAttempts = 3
            };

            try
            {
                generator.NextId("test");
            }
            catch (Exception ex)
            {
                Assert.StartsWith("Failed to update the data store after 3 attempts.", ex.Message);
                return;
            }
            
            Assert.True(false, "NextId should have thrown and been caught in the try block");
        }
    }
}