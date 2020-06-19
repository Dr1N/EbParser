using System;
using System.Threading.Tasks;

namespace EbParser.Interfaces
{
    internal interface IParser
    {
        event EventHandler<Uri> PageChangded;

        event EventHandler<string> Error;

        event EventHandler<string> Report;

        Task ParseAsync();
    }
}