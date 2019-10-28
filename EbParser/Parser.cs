using EbParser.Core;
using EbParser.Interfaces;
using System;
using System.Threading.Tasks;

namespace EbParser
{
    class Parser : IParser
    {
        #region Constants

        private const string Base = "https://ebanoe.it/";

        private const string PostTitleSelector = "h3.entry-title a";

        #endregion

        public event EventHandler<Uri> PageChangded = delegate { };

        public event EventHandler<string> Error = delegate { };

        public async Task ParseAsync()
        {
            using var loader = new PageLoader();
            var content = await loader.LoadPageAsync(Base);
            using var parser = new AngelParser();
            var postTitles = await parser.ParseHtmlAsync(content, PostTitleSelector);
            foreach (var title in postTitles)
            {
                try
                {
                    var postLink = await parser.ParseAttributeAsync(title, "a", "href");
                    var postPage = await loader.LoadPageAsync(postLink);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        #region Private

        #endregion
    }
}