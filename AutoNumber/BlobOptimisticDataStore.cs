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

        // ETag observed by the most recent GetData per block, so TryOptimisticWrite can condition
        // on the state that was actually read. Conditioning on the blob's current ETag (fetched at
        // write time) does not detect writers that committed between our read and our write, which
        // allows two processes to hand out the same id range.
        private readonly ConcurrentDictionary<string, ETag> readETags = new ConcurrentDictionary<string, ETag>();

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

            // DownloadContent returns the content and its ETag from a single response, so the
            // value/ETag pair is consistent
            var download = blobReference.DownloadContent();
            readETags[blockName] = download.Value.Details.ETag;
            return download.Value.Content.ToString();
        }

        public async Task<string> GetDataAsync(string blockName)
        {
            var blobReference = GetBlobReference(blockName);

            var download = await blobReference.DownloadContentAsync().ConfigureAwait(false);
            readETags[blockName] = download.Value.Details.ETag;
            return download.Value.Content.ToString();
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

            // Condition the write on the ETag captured by the preceding GetData, so any writer that
            // committed after our read fails this write with 412 and the caller re-reads and retries.
            // Each read ETag is consumed at most once; callers without a preceding read fall back to
            // the blob's current ETag (previous behaviour).
            if (!readETags.TryRemove(blockName, out var readETag))
            {
                readETag = blobReference.GetProperties().Value.ETag;
            }

            try
            {
                var blobRequestCondition = new BlobRequestConditions
                {
                    IfMatch = readETag
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

            if (!readETags.TryRemove(blockName, out var readETag))
            {
                readETag = (await blobReference.GetPropertiesAsync().ConfigureAwait(false)).Value.ETag;
            }

            try
            {
                var blobRequestCondition = new BlobRequestConditions
                {
                    IfMatch = readETag
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