using System.Net;
using System.Text;
using Microsoft.Azure.Storage;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using AutoNumber.Options;
using AutoNumber.Extensions;
using AutoNumber.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.Azure.Storage.Blob;

namespace AutoNumber
{
    public class BlobOptimisticDataStore : IOptimisticDataStore
    {
        private const string SeedValue = "1";
        private readonly CloudBlobContainer blobContainer;
        private readonly ConcurrentDictionary<string, ICloudBlob> blobReferences;
        private readonly object blobReferencesLock = new object();

        public BlobOptimisticDataStore(CloudStorageAccount account, string containerName)
        {
            var blobClient = account.CreateCloudBlobClient();
            blobContainer = blobClient.GetContainerReference(containerName.ToLower());
            blobReferences = new ConcurrentDictionary<string, ICloudBlob>();
        }

        public BlobOptimisticDataStore(CloudStorageAccount cloudStorageAccount, IOptions<AutoNumberOptions> options)
            : this(cloudStorageAccount, options.Value.StorageContainerName)
        {
        }

        public string GetData(string blockName)
            => GetDataAsync(blockName).GetAwaiter().GetResult();

        public async Task<string> GetDataAsync(string blockName)
        {
            var blobReference = GetBlobReference(blockName);

            using (var stream = new MemoryStream())
            {
                await blobReference.DownloadToStreamAsync(stream).ConfigureAwait(false);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public async Task<bool> Init()
           => await blobContainer.CreateIfNotExistsAsync().ConfigureAwait(false);

        public bool TryOptimisticWrite(string blockName, string data)
            => TryOptimisticWriteAsync(blockName, data).GetAwaiter().GetResult();

        public async Task<bool> TryOptimisticWriteAsync(string blockName, string data)
        {
            var blobReference = GetBlobReference(blockName);
            try
            {
                await UploadTextAsync(
                        blobReference,
                        data,
                        AccessCondition.GenerateIfMatchCondition(blobReference.Properties.ETag)).ConfigureAwait(false);
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

            if (await blobReference.ExistsAsync().ConfigureAwait(false))
                return blobReference;

            try
            {
                await UploadTextAsync(blobReference, SeedValue, AccessCondition.GenerateIfNoneMatchCondition("*")).ConfigureAwait(false);
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
                await blob.UploadFromStreamAsync(stream, accessCondition, null, null).ConfigureAwait(false);
            }
        }
    }
}
