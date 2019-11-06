using EbParser.Context;
using EbParser.DTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EbParser.Interfaces
{
    interface IPostStorage
    {
        Task SaveTagsAsync(IList<string> tags);

        Task<Post> SavePostAsync(PostDto postDto, string url);

        Task SaveCommentsAsync(IList<CommentDto> comments, Post post);

        Task SavePostFilesAsync(IList<string> files);

        string GetLastPostUrl();

        bool IsExists(string url);
    }
}