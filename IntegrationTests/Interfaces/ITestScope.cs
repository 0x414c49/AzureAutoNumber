using System;

namespace IntegrationTests.Interfaces
{
    public interface ITestScope : IDisposable
    {
        string IdScopeName { get; }

        string ReadCurrentPersistedValue();
    }
}