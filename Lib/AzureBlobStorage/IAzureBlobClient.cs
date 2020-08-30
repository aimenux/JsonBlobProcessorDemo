using System.IO;
using System.Threading.Tasks;

namespace Lib.AzureBlobStorage
{
    public interface IAzureBlobClient
    {
        Task<Stream> ReadStreamAsync();
    }
}
