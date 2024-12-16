using System;
using System.IO;
using System.Threading.Tasks;
using AutoNumber.Interfaces;

namespace AutoNumber
{
    internal class DebugOnlyFileDataStore : IOptimisticDataStore
    {
        private const string SeedValue = "1";

        private readonly string directoryPath;

        public DebugOnlyFileDataStore(string directoryPath)
        {
            this.directoryPath = directoryPath;
        }

        public string GetData(string blockName)
        {
            return GetDataWithConcurrencyCheck(blockName).Value;
        }

        public DataWrapper GetDataWithConcurrencyCheck(string blockName)
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

        public Task<string> GetDataAsync(string blockName)
        {
            throw new NotImplementedException();
        }

        public Task<DataWrapper> GetDataWithConcurrencyCheckAsync(string blockName)
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
            return TryOptimisticWriteWithConcurrencyCheck(blockName, data, Azure.ETag.All);
        }

        public bool TryOptimisticWriteWithConcurrencyCheck(string blockName, string data, Azure.ETag eTag)
        {
            var blockPath = Path.Combine(directoryPath, $"{blockName}.txt");

            if(!File.Exists(blockPath)) return false;

            if (!eTag.Equals(Azure.ETag.All))
            {
                var info = new FileInfo(blockPath);
                var eTagToCompare = ETag.ForDate(info.LastWriteTimeUtc);

                if (!eTagToCompare.Equals(eTag)) return false;
            }

            File.WriteAllText(blockPath, data);
            return true;
        }

        public Task<bool> TryOptimisticWriteAsync(string blockName, string data)
        {
            throw new NotImplementedException();
        }

        public Task<bool> TryOptimisticWriteWithConcurrencyCheckAsync(string blockName, string data, Azure.ETag eTag)
        {
            throw new NotImplementedException();
        }
    }
}