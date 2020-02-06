using System.Collections.Generic;
using System.Net;
using System.Text;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace SnowMaker
{
    public class BlobOptimisticDataStore : IOptimisticDataStore
    {
        const string SeedValue = "1";

        readonly CloudBlobContainer blobContainer;

        readonly ConcurrentDictionary<string, ICloudBlob> blobReferences;
        readonly object blobReferencesLock = new object();

        public BlobOptimisticDataStore(CloudStorageAccount account, string containerName)
        {
            var blobClient = account.CreateCloudBlobClient();
            blobContainer = blobClient.GetContainerReference(containerName.ToLower());
            blobReferences = new ConcurrentDictionary<string, ICloudBlob>();
        }

        public string GetData(string blockName)
            => GetDataAsync(blockName).GetAwaiter().GetResult();

        public async Task<string> GetDataAsync(string blockName)
        {
            var blobReference = GetBlobReference(blockName);
            using (var stream = new MemoryStream())
            {
                await blobReference.DownloadToStreamAsync(stream);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public async Task<bool> Init()
           => await blobContainer.CreateIfNotExistsAsync();

        public bool TryOptimisticWrite(string scopeName, string data)
            => TryOptimisticWriteAsync(scopeName, data).GetAwaiter().GetResult();

        public async Task<bool> TryOptimisticWriteAsync(string scopeName, string data)
        {
            var blobReference = GetBlobReference(scopeName);
            try
            {
                await UploadTextAsync(
                        blobReference,
                        data,
                        AccessCondition.GenerateIfMatchCondition(blobReference.Properties.ETag));
            }
            catch (StorageException exc)
            {
                if (exc.RequestInformation.HttpStatusCode == (int)HttpStatusCode.PreconditionFailed)
                    return false;

                throw;
            }
            return true;
        }

        private ICloudBlob GetBlobReference(string blockName)
        {
            return blobReferences.GetValue(
                blockName,
                blobReferencesLock,
                () => InitializeBlobReferenceAsync(blockName).GetAwaiter().GetResult());
        }

        private async Task<ICloudBlob> InitializeBlobReferenceAsync(string blockName)
        {
            var blobReference = blobContainer.GetBlockBlobReference(blockName);

            if (await blobReference.ExistsAsync())
                return blobReference;

            try
            {
                await UploadTextAsync(blobReference, SeedValue, AccessCondition.GenerateIfNoneMatchCondition("*"));
            }
            catch (StorageException uploadException)
            {
                if (uploadException.RequestInformation.HttpStatusCode != (int)HttpStatusCode.Conflict)
                    throw;
            }

            return blobReference;
        }

        private async Task UploadTextAsync(ICloudBlob blob, string text, AccessCondition accessCondition)
        {
            blob.Properties.ContentType = "utf-8";
            blob.Properties.ContentType = "text/plain";
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
            {
                await blob.UploadFromStreamAsync(stream, accessCondition, null, null);
            }
        }
    }
}
