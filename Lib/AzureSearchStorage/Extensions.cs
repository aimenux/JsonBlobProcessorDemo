using System;
using Lib.Configuration;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;

namespace Lib.AzureSearchStorage
{
    public static class Extensions
    {
        public static ISearchIndexClient GetOrCreateSearchIndexClient<T>(this ISearchServiceClient searchServiceClient, AzureSearchSettings settings)
        {
            var indexName = settings.IndexName;

            if (!searchServiceClient.Indexes.Exists(indexName))
            {
                searchServiceClient.CreateIndex<T>(indexName);
            }

            return searchServiceClient.Indexes.GetClient(indexName);
        }

        private static void CreateIndex<T>(this ISearchServiceClient searchServiceClient, string indexName)
        {
            var indexDefinition = new Index
            {
                Name = indexName,
                Fields = FieldBuilder.BuildForType<T>()
            };

            var index = searchServiceClient.Indexes.Create(indexDefinition);
            if (index == null)
            {
                throw new Exception($"Failed to create index {indexName}");
            }
        }
    }
}