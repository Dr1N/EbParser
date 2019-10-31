using System.Threading.Tasks;

namespace EbParser.Interfaces
{
    interface IPageLoader
    {
        Task<string> LoadPageAsync(string url);

        Task<bool> LoadFileAsync(string url, string path);
    }
}