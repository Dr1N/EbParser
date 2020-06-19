using System.Threading.Tasks;

namespace EbParser.Interfaces
{
    internal interface ILoader
    {
        Task<string> LoadPageAsync(string url);

        Task<bool> LoadFileAsync(string url, string path);
    }
}