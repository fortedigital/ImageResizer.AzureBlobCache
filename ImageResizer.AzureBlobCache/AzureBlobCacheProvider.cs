using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Forte.ImageResizer.AzureBlobCache
{
    public class AzureBlobCacheProvider
    {
        private readonly CloudBlobContainer container;
        private readonly ISimpleLogger logger;

        public AzureBlobCacheProvider(CloudBlobContainer container, ISimpleLogger logger)
        {
            this.container = container;
            this.logger = logger;
        }

        public async Task<Stream> ResolveAsync(string blobName, Func<Stream,Task> generate)
        {
            var blob = this.GetBlobReference(blobName);

            try
            {
                return await blob.OpenReadAsync();
            }
            catch (StorageException ex) when (IsBlobNotFoundException(ex))
            {
            }
            catch (StorageException ex)
            {
                this.logger.LogError($"Failed to download blob {blobName}", ex);
            }

            var outputStream = new MemoryStream();
            await generate(outputStream);
            
            try
            {
                outputStream.Position = 0;
                await blob.UploadFromStreamAsync(outputStream);
            }
            catch (Exception ex)
            {
                // ignore errors like conflicts etc.
                this.logger.LogError($"Failed to upload to blob {blobName}. Item will not be cached.", ex);
            }

            outputStream.Position = 0;
            return outputStream;
        }

        public Stream Resolve(string blobName, Action<Stream> generate)
        {
            var blob = this.GetBlobReference(blobName);

            try
            {
                return blob.OpenRead();
            }
            catch (StorageException ex) when (IsBlobNotFoundException(ex))
            {
            }
            catch (StorageException ex)
            {
                this.logger.LogError($"Failed to download blob {blobName}", ex);
            }

            var outputStream = new MemoryStream();
            generate(outputStream);
            
            try
            {
                outputStream.Position = 0;
                blob.UploadFromStream(outputStream);
            }
            catch (Exception ex)
            {
                // ignore errors like conflicts etc.
                this.logger.LogError($"Failed to upload to blob {blobName}. Item will not be cached.", ex);
            }

            outputStream.Position = 0;
            return outputStream;
        }

        private static bool IsBlobNotFoundException(StorageException ex)
        {
            return ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound;
        }

        private CloudBlockBlob GetBlobReference(string blobName)
        {
            return this.container.GetBlockBlobReference(blobName);
        }
    }
}