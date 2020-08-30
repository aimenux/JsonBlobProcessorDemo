using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Lib.Configuration;
using Microsoft.Extensions.Options;

namespace Lib.AzureBlobStorage
{
    public class AzureBlobClient : IAzureBlobClient
    {
        private readonly AzureBlobSettings _settings;

        public AzureBlobClient(IOptions<Settings> options)
        {
            _settings = options.Value.AzureBlobSettings;
        }

        public async Task<Stream> ReadStreamAsync()
        {
            var blobClient = new BlobClient(_settings.ConnectionString, _settings.ContainerName, _settings.BlobName);
            var exists = await blobClient.ExistsAsync();
            if (!exists)
            {
                throw new FileNotFoundException($"Unfound blob '{_settings.BlobName}'");
            }

            var memoryStream = new MemoryStream();
            await blobClient.DownloadToAsync(memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }
    }
}