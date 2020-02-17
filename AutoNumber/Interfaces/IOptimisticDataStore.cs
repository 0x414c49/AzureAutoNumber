using System.Threading.Tasks;

namespace AutoNumber.Interfaces
{
    public interface IOptimisticDataStore
    {
        string GetData(string blockName);
        Task<string> GetDataAsync(string blockName);
        bool TryOptimisticWrite(string blockName, string data);
        Task<bool> TryOptimisticWriteAsync(string blockName, string data);
        Task<bool> Init();
    }
}
