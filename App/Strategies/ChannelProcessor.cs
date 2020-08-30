using System.Collections.Generic;
using System.IO;
using System.Threading.Channels;
using System.Threading.Tasks;
using Lib.AzureBlobStorage;
using Lib.AzureSearchStorage;
using Lib.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.Strategies
{
    public class ChannelProcessor : IProcessor
    {
        private const int BatchSize = 32_000;
        private const int ChannelSize = 5_000;
        private readonly IAzureBlobClient _azureBlobClient;
        private readonly IAzureSearchClient<PersonIndex> _azureSearchClient;
        private readonly ILogger _logger;

        public ChannelProcessor(IAzureBlobClient azureBlobClient, IAzureSearchClient<PersonIndex> azureSearchClient, ILogger logger)
        {
            _azureBlobClient = azureBlobClient;
            _azureSearchClient = azureSearchClient;
            _logger = logger;
        }

        public string Name => GetType().Name;

        public async Task LaunchAsync()
        {
            var options = new BoundedChannelOptions(ChannelSize)
            {
                SingleReader = true,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.Wait
            };

            var channel = Channel.CreateBounded<Person>(options);

            var producer = Task.Run(async () =>
            {
                await using var stream = await _azureBlobClient.ReadStreamAsync();
                using var sr = new StreamReader(stream);
                using var reader = new JsonTextReader(sr);
                while (await reader.ReadAsync())
                {
                    if (reader.TokenType == JsonToken.StartObject)
                    {
                        var obj = await JObject.LoadAsync(reader);
                        var person = obj.ToObject<Person>();
                        await channel.Writer.WriteAsync(person);
                    }
                }

                channel.Writer.Complete();

                _logger.LogInformation("Producer has complete");
            });

            var consumer = Task.Run(async () =>
            {
                while (await channel.Reader.WaitToReadAsync())
                {
                    var searchablePersons = new List<SearchablePerson>();

                    await foreach (var person in channel.Reader.ReadAllAsync())
                    {
                        searchablePersons.Add(new SearchablePerson(person));
                    }

                    foreach (var searchablePersonsBatch in searchablePersons.Batch(BatchSize))
                    {
                        await _azureSearchClient.SaveAsync(searchablePersonsBatch);
                    }
                }

                _logger.LogInformation("Consumer has complete");
            });

            await Task.WhenAll(producer, consumer);
        }
    }
}
