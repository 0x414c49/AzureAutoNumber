using System;
using System.IO;
using AutoNumber;
using AutoNumber.Interfaces;
using NUnit.Framework;

namespace IntegrationTests.cs
{
    [TestFixture]
    public class File : Scenarios<File.TestScope>
    {
        protected override TestScope BuildTestScope()
        {
            return new TestScope();
        }

        protected override IOptimisticDataStore BuildStore(TestScope scope)
        {
            return new DebugOnlyFileDataStore(scope.DirectoryPath);
        }

        public class TestScope : ITestScope
        {
            public TestScope()
            {
                var ticks = DateTime.UtcNow.Ticks;
                IdScopeName = string.Format("AutoNumbertest{0}", ticks);

                DirectoryPath = Path.Combine(Path.GetTempPath(), IdScopeName);
                Directory.CreateDirectory(DirectoryPath);
            }

            public string DirectoryPath { get; }

            public string IdScopeName { get; }

            public string ReadCurrentPersistedValue()
            {
                var filePath = Path.Combine(DirectoryPath, string.Format("{0}.txt", IdScopeName));
                return System.IO.File.ReadAllText(filePath);
            }

            public void Dispose()
            {
                if (Directory.Exists(DirectoryPath))
                    Directory.Delete(DirectoryPath, true);
            }
        }
    }
}