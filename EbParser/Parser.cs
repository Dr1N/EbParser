using System;
using System.Threading.Tasks;
using EbParser.Interfaces;

namespace EbParser
{
    class Parser : IParser
    {
        public event EventHandler<Uri> PageChangded;

        public event EventHandler<string> Error;

        public Task ParseAsync()
        {
            throw new NotImplementedException();
        }
    }
}