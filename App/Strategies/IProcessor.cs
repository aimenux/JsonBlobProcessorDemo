using System.Threading.Tasks;

namespace App.Strategies
{
    public interface IProcessor
    {
        Task LaunchAsync();
    }
}
