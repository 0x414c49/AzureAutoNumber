using System.Collections.Generic;
using System.Threading;
using AutoNumber.Extensions;
using Xunit;

namespace AutoNumber.UnitTests
{
    public class DictionaryExtentionsTests
    {
        private static bool IsLockedOnCurrentThread(object lockObject)
        {
            var reset = new ManualResetEvent(false);
            var couldLockBeAcquiredOnOtherThread = false;
            new Thread(() =>
            {
                couldLockBeAcquiredOnOtherThread = Monitor.TryEnter(lockObject, 0);
                reset.Set();
            }).Start();
            reset.WaitOne();
            return !couldLockBeAcquiredOnOtherThread;
        }

        [Fact]
        public void GetValueShouldCallTheValueInitializerWithinTheLockIfTheKeyDoesntExist()
        {
            var dictionary = new Dictionary<string, string>
            {
                {"foo", "bar"}
            };

            var dictionaryLock = new object();

            dictionary.GetValue(
                "bar",
                dictionaryLock,
                () =>
                {
                    Assert.True(IsLockedOnCurrentThread(dictionaryLock));
                    return "qak";
                });
        }

        [Fact]
        public void GetValueShouldReturnExistingValueWithoutUsingTheLock()
        {
            var dictionary = new Dictionary<string, string>
            {
                {"foo", "bar"}
            };

            var value = dictionary.GetValue("foo", null, null);

            Assert.Equal("bar", value);
        }

        [Fact]
        public void GetValueShouldStoreNewValuesAfterCallingTheValueInitializerOnce()
        {
            var dictionary = new Dictionary<string, string>
            {
                {"foo", "bar"}
            };

            var dictionaryLock = new object();

            dictionary.GetValue("bar", dictionaryLock, () => "qak");

            dictionary.GetValue(
                "bar",
                dictionaryLock,
                () =>
                {
                    Assert.Fail("Value initializer should not have been called a second time.");
                    return null;
                });
        }
    }
}
