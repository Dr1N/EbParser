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

        #endregion

        public event EventHandler<Uri> PageChangded = delegate { };

        public event EventHandler<string> Error = delegate { };

        public async Task ParseAsync()
        {
            string page1 = "https://ebanoe.it/";
            string page2 = "https://ebanoe.it/page/2/";
            using var loader = new PageLoader();
            var html1 = await loader.LoadPageAsync(page1);
            var html2 = await loader.LoadPageAsync(page2);
        }
    }
}