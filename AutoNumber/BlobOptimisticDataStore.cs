using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AutoNumber.Extensions;
using AutoNumber.Interfaces;
using AutoNumber.Options;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Options;

namespace AutoNumber
{
    public class BlobOptimisticDataStore : IOptimisticDataStore
    {
        private const string SeedValue = "1";
        private readonly BlobContainerClient blobContainer;
        private readonly ConcurrentDictionary<string, BlockBlobClient> blobReferences;
        private readonly object blobReferencesLock = new object();

        public BlobOptimisticDataStore(BlobServiceClient blobServiceClient, string containerName)
        {
            blobContainer = blobServiceClient.GetBlobContainerClient(containerName.ToLower());
            blobReferences = new ConcurrentDictionary<string, BlockBlobClient>();
        }

        public BlobOptimisticDataStore(BlobServiceClient blobServiceClient, IOptions<AutoNumberOptions> options)
            : this(blobServiceClient, options.Value.StorageContainerName)
        {
        }

        public string GetData(string blockName)
        {
            var blobReference = GetBlobReference(blockName);

            using (var stream = new MemoryStream())
            {
                blobReference.DownloadTo(stream);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public async Task<string> GetDataAsync(string blockName)
        {
            var blobReference = GetBlobReference(blockName);

            using (var stream = new MemoryStream())
            {
                await blobReference.DownloadToAsync(stream).ConfigureAwait(false);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public async Task<bool> InitAsync()
        {
            var result = await blobContainer.CreateIfNotExistsAsync().ConfigureAwait(false);
            return result == null || result.Value != null;
        }

        public bool Init()
        {
            var result = blobContainer.CreateIfNotExists();
            return result == null || result.Value != null;
        }

        public bool TryOptimisticWrite(string blockName, string data)
        {
            var blobReference = GetBlobReference(blockName);
            try
            {
                var blobRequestCondition = new BlobRequestConditions
                {
                    IfMatch = (blobReference.GetProperties()).Value.ETag
                };
                UploadText(
                    blobReference,
                    data,
                    blobRequestCondition);
            }
            catch (RequestFailedException exc)
            {
                if (exc.Status == (int)HttpStatusCode.PreconditionFailed)
                    return false;

                throw;
            }

            return true;
        }

        public async Task<bool> TryOptimisticWriteAsync(string blockName, string data)
        {
            var blobReference = GetBlobReference(blockName);
            try
            {
                var blobRequestCondition = new BlobRequestConditions
                {
                    IfMatch = (await blobReference.GetPropertiesAsync()).Value.ETag
                };
                await UploadTextAsync(
                    blobReference,
                    data,
                    blobRequestCondition).ConfigureAwait(false);
            }
            catch (RequestFailedException exc)
            {
                if (exc.Status == (int)HttpStatusCode.PreconditionFailed)
                    return false;

                throw;
            }

            return true;
        }

        private BlockBlobClient GetBlobReference(string blockName)
        {
            return blobReferences.GetValue(
                blockName,
                blobReferencesLock,
                () => InitializeBlobReference(blockName));
        }

        private BlockBlobClient InitializeBlobReference(string blockName)
        {
            var blobReference = blobContainer.GetBlockBlobClient(blockName);

            if (blobReference.Exists())
                return blobReference;

            try
            {
                var blobRequestCondition = new BlobRequestConditions
                {
                    IfNoneMatch = ETag.All
                };
                UploadText(blobReference, SeedValue, blobRequestCondition);
            }
            catch (RequestFailedException uploadException)
            {
                if (uploadException.Status != (int)HttpStatusCode.Conflict)
                    throw;
            }

            return blobReference;
        }

        private async Task UploadTextAsync(BlockBlobClient blob, string text, BlobRequestConditions accessCondition)
        {
            var header = new BlobHttpHeaders
            {
                ContentType = "text/plain"
            };

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
            {
                await blob.UploadAsync(stream, header, null, accessCondition, null, null).ConfigureAwait(false);
            }
        }

        private void UploadText(BlockBlobClient blob, string text, BlobRequestConditions accessCondition)
        {
            var header = new BlobHttpHeaders
            {
                ContentType = "text/plain"
            };

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
            {
                blob.Upload(stream, header, null, accessCondition, null, null);
            }
        }
    }
}