using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EbParser.Interfaces
{
    interface ISaver<T> : IDisposable
    {
        Task SaveAsync(IList<T> items);
    }
}