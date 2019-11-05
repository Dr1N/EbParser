using EbParser.Context;
using EbParser.DTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EbParser.Interfaces
{
    interface IPostStorage
    {
        Task<IList<Tag>> SaveTagsAsync(IList<string> tags);

        Task<Post> SavePostAsync(PostDto postDto, string url);

        Task<IList<Comment>> SaveCommentsAsync(IList<CommentDto> comments, Post post);

        Task<IList<File>> SavePostFilesAsync(IList<string> files);

        string GetLastPostUrl();

        bool IsExists(string url);
    }
}