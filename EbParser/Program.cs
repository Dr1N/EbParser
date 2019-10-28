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

            using var worker = new Parser();
            worker.PageChangded += Worker_PageChangded;
            worker.Error += Worker_Error;
            await worker.ParseAsync();

            Console.WriteLine($"Time: {stopWatch.Elapsed.TotalSeconds} sec");
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