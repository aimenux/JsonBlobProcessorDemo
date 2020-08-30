using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lib.AzureSearchStorage
{
    public interface IAzureSearchClient<TAzureSearchIndex> where TAzureSearchIndex : IAzureSearchIndex
    {
        Task<long> CountAsync();
        Task DeleteIndexAndDocumentsAsync();
        Task CreateIndexWhenNotExistsAsync();
        Task SaveAsync<TAzureSearchModel>(TAzureSearchModel model) where TAzureSearchModel : IAzureSearchModel;
        Task SaveAsync<TAzureSearchModel>(IEnumerable<TAzureSearchModel> models) where TAzureSearchModel : IAzureSearchModel;
    }
}
