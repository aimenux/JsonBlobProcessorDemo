using System.Threading.Tasks;

namespace App.Strategies
{
    public interface IProcessor
    {
        string Name { get; }

        Task LaunchAsync();
    }
}
