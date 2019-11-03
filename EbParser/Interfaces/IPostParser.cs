using System.Collections.Generic;
using System.Threading.Tasks;
using EbParser.DTO;

namespace EbParser.Interfaces
{
    interface IPostParser
    {
        Task<IList<CommentDto>> GetPostCommentsAsync();
        
        Task<PostDto> GetPostDtoAsync();
        
        Task<IList<string>> GetPostFilesAsync();
    }
}