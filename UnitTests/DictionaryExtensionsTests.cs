using System.Collections.Generic;
using System.Threading;
using AutoNumber.Extensions;
using Xunit;

namespace AutoNumber.UnitTests
{
    public class DictionaryExtensionsTests
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
        public void GetValue_Should_Call_TheValueInitializer_Within_TheLock_If_TheKey_Doesnt_Exist()
        {
            var dictionary = new Dictionary<string, string>
            {
                {"foo", "bar"}
            };

            var dictionaryLock = new object();

            // Act
            dictionary.GetValue(
                "bar",
                dictionaryLock,
                () =>
                {
                    // Assert
                    Assert.True(IsLockedOnCurrentThread(dictionaryLock));
                    return "qak";
                });
        }

        [Fact]
        public void GetValue_Should_Return_Existing_Value_Without_Using_TheLock()
        {
            var dictionary = new Dictionary<string, string>
            {
                {"foo", "bar"}
            };

            // Act
            // null can't be used as a lock and will throw an exception if attempted
            var value = dictionary.GetValue("foo", null, null);

            // Assert
            Assert.Equal("bar", value);
        }

        [Fact]
        public void GetValue_Should_Store_NewValues_After_Calling_TheValueInitializer_Once()
        {
            var dictionary = new Dictionary<string, string>
            {
                {"foo", "bar"}
            };

            var dictionaryLock = new object();

            // Arrange
            dictionary.GetValue("bar", dictionaryLock, () => "qak");

            // Act
            dictionary.GetValue(
                "bar",
                dictionaryLock,
                () =>
                {
                    // Assert
                    Assert.True(false, "Value initializer should not have been called a second time.");
                    return null;
                });
        }
    }
}