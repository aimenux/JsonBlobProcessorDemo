using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lib.Configuration;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Extensions.Options;

namespace Lib.AzureSearchStorage
{
    public class AzureSearchClient<TAzureSearchIndex> : IAzureSearchClient<TAzureSearchIndex> where TAzureSearchIndex : IAzureSearchIndex
    {
        private readonly AzureSearchSettings _settings;
        private readonly ISearchIndexClient _searchIndexClient;
        private readonly SearchServiceClient _searchServiceClient;

        public AzureSearchClient(IOptions<Settings> options)
        {
            _settings = options.Value.AzureSearchSettings;
            var credentials = new SearchCredentials(_settings.ApiKey);
            _searchServiceClient = new SearchServiceClient(_settings.ServiceName, credentials);
            _searchIndexClient = GetSearchIndexClient();
        }

        public Task<long> CountAsync()
        {
            return _searchIndexClient.Documents.CountAsync();
        }

        public Task DeleteIndexAndDocumentsAsync()
        {
            return _searchServiceClient.Indexes.DeleteAsync(_settings.IndexName);
        }

        public async Task CreateIndexWhenNotExistsAsync()
        {
            var indexName = _settings.IndexName;
            var exists = await _searchServiceClient.Indexes.ExistsAsync(indexName);
            if (exists)
            {
                return;
            }

            var fields = FieldBuilder.BuildForType<TAzureSearchIndex>();
            var index = new Index
            {
                Name = indexName,
                Fields = fields
            };

            await _searchServiceClient.Indexes.CreateAsync(index);
        }

        public Task SaveAsync<TAzureSearchModel>(TAzureSearchModel model) where TAzureSearchModel : IAzureSearchModel
        {
            return SaveAsync(new List<TAzureSearchModel> {model});
        }

        public Task SaveAsync<TAzureSearchModel>(IEnumerable<TAzureSearchModel> models) where TAzureSearchModel : IAzureSearchModel
        {
            if (models == null)
            {
                return Task.CompletedTask;
            }

            var actions = models.Select(IndexAction.Upload);
            var batch = IndexBatch.New(actions);
            return _searchIndexClient.Documents.IndexAsync(batch);
        }

        private ISearchIndexClient GetSearchIndexClient()
        {
            CreateIndexWhenNotExistsAsync().GetAwaiter().GetResult();
            return _searchServiceClient.Indexes.GetClient(_settings.IndexName);
        }
    }
}