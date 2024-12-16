using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using Azure;

namespace AutoNumber.Interfaces
{
    public interface IOptimisticDataStore
    {
        string GetData(string blockName);
        DataWrapper GetDataWithConcurrencyCheck(string blockName);

        Task<string> GetDataAsync(string blockName);
        Task<DataWrapper> GetDataWithConcurrencyCheckAsync(string blockName);

        bool TryOptimisticWrite(string blockName, string data);
        bool TryOptimisticWriteWithConcurrencyCheck(string blockName, string data, Azure.ETag eTag);

        Task<bool> TryOptimisticWriteAsync(string blockName, string data);

        Task<bool> TryOptimisticWriteWithConcurrencyCheckAsync(string blockName, string data, Azure.ETag eTag);
        Task<bool> InitAsync();
        bool Init();
    }
}