using AutoNumber.Interfaces;
using System.IO;
using System.Threading.Tasks;

namespace AutoNumber
{
    public class DebugOnlyFileDataStore : IOptimisticDataStore
    {
        const string SeedValue = "1";

        readonly string directoryPath;

        public DebugOnlyFileDataStore(string directoryPath)
        {
            this.directoryPath = directoryPath;
        }

        public string GetData(string blockName)
        {
            var blockPath = Path.Combine(directoryPath, $"{blockName}.txt");
            try
            {
                return File.ReadAllText(blockPath);
            }
            catch (FileNotFoundException)
            {
                FileStream file = File.Create(blockPath);

                using (var streamWriter = new StreamWriter(file))
                {
                    streamWriter.Write(SeedValue);
                }

                return SeedValue;
            }
        }

        public Task<string> GetDataAsync(string blockName)
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> Init()
        {
            return Task.FromResult(true);
        }

        public bool TryOptimisticWrite(string blockName, string data)
        {
            var blockPath = Path.Combine(directoryPath, $"{blockName}.txt");
            File.WriteAllText(blockPath, data);
            return true;
        }

        public Task<bool> TryOptimisticWriteAsync(string blockName, string data)
        {
            throw new System.NotImplementedException();
        }
    }
}
