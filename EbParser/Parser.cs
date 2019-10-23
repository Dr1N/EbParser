using System;
using System.Diagnostics;
using System.Threading.Tasks;
using EbParser.Core;
using EbParser.Interfaces;

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
            var loader = new PageLoader();
            var page = await loader.LoadPageAsync(Base);

            Debug.WriteLine(page.Length);
        }
    }
}