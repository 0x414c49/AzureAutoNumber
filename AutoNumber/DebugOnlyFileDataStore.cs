using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using AutoNumber.Interfaces;
using Azure;

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

        public DataWrapper GetData(string blockName)
        {
            var blockPath = Path.Combine(directoryPath, $"{blockName}.txt");
            try
            {
                var info = new FileInfo(blockPath);
                return new DataWrapper(File.ReadAllText(blockPath), ETag.ForDate(info.LastWriteTimeUtc));
            }
            catch (FileNotFoundException)
            {
                var file = File.Create(blockPath);

                using (var streamWriter = new StreamWriter(file))
                {
                    streamWriter.Write(SeedValue);
                }

                var info = new FileInfo(blockPath);

                return new DataWrapper(SeedValue, ETag.ForDate(info.LastWriteTimeUtc));
            }
        }

        public Task<DataWrapper> GetDataAsync(string blockName)
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

        public bool TryOptimisticWrite(string blockName, string data, Azure.ETag eTag)
        {
            var blockPath = Path.Combine(directoryPath, $"{blockName}.txt");
            File.WriteAllText(blockPath, data);
            return true;
        }

        public Task<bool> TryOptimisticWriteAsync(string blockName, string data, Azure.ETag eTag)
        {
            throw new NotImplementedException();
        }
    }
}