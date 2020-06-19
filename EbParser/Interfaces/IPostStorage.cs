using EbParser.DTO;
using System.Threading.Tasks;

namespace EbParser.Interfaces
{
    internal interface IPostStorage
    {
        Task SavePostAsync(string url, PostDto postDto);

        string GetLastPostUrl();

        bool IsExists(string url);
    }
}