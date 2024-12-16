using System;

namespace AutoNumber.IntegrationTests
{
    public interface ITestScope : IDisposable
    {
        string IdScopeName { get; }

        string ReadCurrentPersistedValue();
    }
}