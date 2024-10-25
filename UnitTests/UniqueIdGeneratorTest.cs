using System;
using AutoNumber.Exceptions;
using AutoNumber.Interfaces;
using NSubstitute;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace AutoNumber.UnitTests
{
    [TestFixture]
    public class UniqueIdGeneratorTest
    {
        [Test]
        public void ConstructorShouldNotRetrieveDataFromStore()
        {
            var store = Substitute.For<IOptimisticDataStore>();
            // ReSharper disable once ObjectCreationAsStatement
            new UniqueIdGenerator(store);
            store.DidNotReceiveWithAnyArgs().GetData(null);
        }

        [Test]
        public void MaxWriteAttemptsShouldThrowArgumentOutOfRangeExceptionWhenValueIsNegative()
        {
            var store = Substitute.For<IOptimisticDataStore>();
            Assert.That(() =>
                    // ReSharper disable once ObjectCreationAsStatement
                    new UniqueIdGenerator(store)
                    {
                        MaxWriteAttempts = -1
                    }
                , Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void MaxWriteAttemptsShouldThrowArgumentOutOfRangeExceptionWhenValueIsZero()
        {
            var store = Substitute.For<IOptimisticDataStore>();
            Assert.That(() =>
                    // ReSharper disable once ObjectCreationAsStatement
                    new UniqueIdGenerator(store)
                    {
                        MaxWriteAttempts = 0
                    }
                , Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void NextIdShouldReturnNumbersSequentially()
        {
            var store = Substitute.For<IOptimisticDataStore>();
            store.GetData("test").Returns("0", "250");
            store.TryOptimisticWrite("test", "3").Returns(true);

            var subject = new UniqueIdGenerator(store)
            {
                BatchSize = 3
            };

            Assert.That(subject.NextId("test"), Is.EqualTo(0));
            Assert.That(subject.NextId("test"), Is.EqualTo(1));
            Assert.That(subject.NextId("test"), Is.EqualTo(2));
        }

        [Test]
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

            Assert.That(subject.NextId("test"), Is.EqualTo(0));
            Assert.That(subject.NextId("test"), Is.EqualTo(1));
            Assert.That(subject.NextId("test"), Is.EqualTo(2)  );
            Assert.That(subject.NextId("test"), Is.EqualTo(250));
            Assert.That(subject.NextId("test"), Is.EqualTo(251));
            Assert.That(subject.NextId("test"), Is.EqualTo(252));
        }

        [Test]
        public void NextIdShouldThrowExceptionOnCorruptData()
        {
            var store = Substitute.For<IOptimisticDataStore>();
            store.GetData("test").Returns("abc");

            Assert.That(() =>
                {
                    var generator = new UniqueIdGenerator(store);
                    generator.NextId("test");
                }
                , Throws.TypeOf<UniqueIdGenerationException>());
        }

        [Test]
        public void NextIdShouldThrowExceptionOnNullData()
        {
            var store = Substitute.For<IOptimisticDataStore>();
            store.GetData("test").Returns((string) null);

            Assert.That(() =>
                {
                    var generator = new UniqueIdGenerator(store);
                    generator.NextId("test");
                }
                , Throws.TypeOf<UniqueIdGenerationException>());
        }

        [Test]
        public void NextIdShouldThrowExceptionWhenRetriesAreExhausted()
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
                StringAssert.StartsWith("Failed to update the data store after 3 attempts.", ex.Message);
                return;
            }

            Assert.Fail("NextId should have thrown and been caught in the try block");
        }
    }
}