using System;
using System.Threading.Tasks;

namespace EbParser.Interfaces
{
    interface IParser
    {
        event EventHandler<Uri> PageChangded;

        event EventHandler<string> Error;

        Task ParseAsync();
    }
}