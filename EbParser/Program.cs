using System;
using System.Diagnostics;

namespace EbParser
{
    class Program
    {
        static async void Main(string[] args)
        {
            var stopWatch = Stopwatch.StartNew();
           
            var worker = new Parser();
            worker.PageChangded += Worker_PageChangded;
            worker.Error += Worker_Error;
            await worker.ParseAsync();

            Console.WriteLine($"Time: {stopWatch.Elapsed.TotalSeconds} sec");
        }

        private static void Worker_Error(object sender, string e)
        {
            throw new NotImplementedException();
        }

        private static void Worker_PageChangded(object sender, Uri e)
        {
            throw new NotImplementedException();
        }
    }
}
