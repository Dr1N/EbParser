using System.Collections.Generic;
using System.Threading.Tasks;

namespace EbParser.Interfaces
{
    interface IHtmlParser
    {
        Task<IList<string>> ParseHtmlAsync(string html, string selector);

        Task<string> ParseAttributeAsync(string html, string selector, string attribute);

        Task<string> ParseTextAsync(string html, string selector);
    }
}