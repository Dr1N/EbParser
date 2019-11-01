using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace EbParser
{
    class Program
    {
        private const string SaveFilesArg = "-f";

        static void Main(string[] args)
        {
            try
            {
                MainAsync(args)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }
            catch { }

            Console.ReadLine();
        }

        static async Task MainAsync(string[] args)
        {
            var stopWatch = Stopwatch.StartNew();

            var saveFiles = args.Any(a => a == SaveFilesArg);

            using var worker = new Parser(saveFiles);
            worker.PageChangded += Worker_PageChangded;
            worker.Error += Worker_Error;
            worker.Report += Worker_Report;
            await worker.ParseAsync();

            Console.WriteLine($"Time: {stopWatch.Elapsed.TotalSeconds} sec");
        }

        private static void Worker_Report(object sender, string e)
        {
            Print($"Info: {e}");
        }

        #region Callbacks

        private static void Worker_Error(object sender, string e)
        {
            Print($"Error: {e}", ConsoleColor.Red);
        }

        private static void Worker_PageChangded(object sender, Uri e)
        {
            Print($"Page: {e.AbsoluteUri}");
        }

        #endregion

        #region Private

        private static void Print(string message, ConsoleColor color = ConsoleColor.White)
        {
            var dt = DateTime.Now;
            lock(typeof(Console))
            {
                Console.ForegroundColor = color;
                try
                {
                    Console.WriteLine("{0, -10}{1}", dt.ToLongTimeString(), message);
                }
                finally
                {
                    Console.ResetColor();
                }
            }
        }

        #endregion
    }
}