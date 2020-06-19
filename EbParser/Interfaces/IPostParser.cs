using System.Collections.Generic;
using System.Threading.Tasks;
using EbParser.DTO;

namespace EbParser.Interfaces
{
    internal interface IPostParser
    {
        Task<IList<CommentDto>> GetPostCommentsAsync();

        Task<PostDto> GetPostDtoAsync();

        Task<IList<string>> GetPostFilesAsync();
    }
}