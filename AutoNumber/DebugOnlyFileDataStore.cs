using System;
using System.IO;
using System.Threading.Tasks;
using AutoNumber.Interfaces;

namespace AutoNumber
{
    public class DebugOnlyFileDataStore : IOptimisticDataStore
    {
        private const string SeedValue = "1";

        private readonly string directoryPath;

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
                var file = File.Create(blockPath);

                using (var streamWriter = new StreamWriter(file))
                {
                    streamWriter.Write(SeedValue);
                }

                return SeedValue;
            }
        }

        public Task<string> GetDataAsync(string blockName)
        {
            throw new NotImplementedException();
        }

        public Task<bool> InitAsync()
        {
            return Task.FromResult(true);
        }

        public bool Init()
        {
            return true;
        }

        public bool TryOptimisticWrite(string blockName, string data)
        {
            var blockPath = Path.Combine(directoryPath, $"{blockName}.txt");
            File.WriteAllText(blockPath, data);
            return true;
        }

        public Task<bool> TryOptimisticWriteAsync(string blockName, string data)
        {
            throw new NotImplementedException();
        }
    }
}