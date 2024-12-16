using System.Threading.Tasks;
using Azure;

namespace AutoNumber.Interfaces
{
    public interface IOptimisticDataStore
    {
        DataWrapper GetData(string blockName);
        Task<DataWrapper> GetDataAsync(string blockName);
        bool TryOptimisticWrite(string blockName, string data, Azure.ETag eTag);
        Task<bool> TryOptimisticWriteAsync(string blockName, string data, Azure.ETag eTag);
        Task<bool> InitAsync();
        bool Init();
    }

    public class DataWrapper
    {
        public DataWrapper(string value, Azure.ETag eTag)
        {
            Value = value;
            ETag = eTag;
        }

        public Azure.ETag ETag { get; private set; }

        public string Value { get; private set; }
    }
}