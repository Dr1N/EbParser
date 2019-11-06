using EbParser.Context;
using EbParser.DTO;
using System.Threading.Tasks;

namespace EbParser.Interfaces
{
    interface IPostStorage
    {
        Task SavePostAsync(string url, PostDto postDto);

        string GetLastPostUrl();

        bool IsExists(string url);
    }
}