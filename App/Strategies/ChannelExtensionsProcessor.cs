using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Lib.AzureBlobStorage;
using Lib.AzureSearchStorage;
using Lib.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Open.ChannelExtensions;

namespace App.Strategies
{
    public class ChannelExtensionsProcessor : IProcessor
    {
        private const int BatchSize = 32_000;
        private const int ChannelSize = 5_000;
        private readonly IAzureBlobClient _azureBlobClient;
        private readonly IAzureSearchClient<PersonIndex> _azureSearchClient;
        private readonly ILogger _logger;

        public string Name => GetType().Name;

        public ChannelExtensionsProcessor(IAzureBlobClient azureBlobClient, IAzureSearchClient<PersonIndex> azureSearchClient, ILogger logger)
        {
            _azureBlobClient = azureBlobClient;
            _azureSearchClient = azureSearchClient;
            _logger = logger;
        }

        public async Task LaunchAsync()
        {
            await Channel.CreateBounded<Person>(ChannelSize)
                .Source(GetPersonsAsync())
                .Batch(BatchSize)
                .Transform(persons => persons.Select(x => new SearchablePerson(x)))
                .ReadAllAsync(async searchablePersonsBatch => await _azureSearchClient.SaveAsync(searchablePersonsBatch));

            _logger.LogInformation("Producer/Consumer has complete");
        }

        private async IAsyncEnumerable<Person> GetPersonsAsync()
        {
            await using var stream = await _azureBlobClient.ReadStreamAsync();
            using var sr = new StreamReader(stream);
            using var reader = new JsonTextReader(sr);
            while (await reader.ReadAsync())
            {
                if (reader.TokenType != JsonToken.StartObject)
                {
                    continue;
                }

                var obj = await JObject.LoadAsync(reader);
                var person = obj.ToObject<Person>();
                yield return person;
            }
        }
    }
}
