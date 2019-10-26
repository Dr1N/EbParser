using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace EbParser
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch { }

            Console.ReadLine();
        }

        static async Task MainAsync(string[] args)
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
        }

        private static void Worker_PageChangded(object sender, Uri e)
        {
        }
    }
}