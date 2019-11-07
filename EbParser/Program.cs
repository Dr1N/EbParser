using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EbParser
{
    class Program
    {
        private static readonly string Separator = new string('=', 50);
        private const string SaveFilesArg = "-f";
        private const string StartPageArg = "-p=";
        private static StringBuilder _sb;

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
            _sb = new StringBuilder();
            var stopWatch = Stopwatch.StartNew();

            var saveFiles = args.Any(a => a == SaveFilesArg);
            var startPage = args.FirstOrDefault(a => a.Contains(StartPageArg))?.Split("=").Last();
            int.TryParse(startPage, out int page);
            using var parser = new Parser(saveFiles, page);
            parser.PageChangded += Worker_PageChangded;
            parser.Error += Worker_Error;
            parser.Report += Worker_Report;
            await parser.ParseAsync();
            File.WriteAllText("parse.log", _sb.ToString());
            _sb.Clear();

            Console.WriteLine($"Time: { stopWatch.Elapsed.TotalSeconds } sec");
        }

        #region Callbacks

        private static void Worker_Report(object sender, string e)
        {
            Print($"Info: { e }");
        }

        private static void Worker_Error(object sender, string e)
        {
            Print($"Error: { e }", ConsoleColor.Red);
        }

        private static void Worker_PageChangded(object sender, Uri e)
        {
            Print(Separator);
            Print($"Page: { e.AbsoluteUri }");
            Print(Separator);
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
                    var msg = string.Format("{0, -10}{1}", dt.ToLongTimeString(), message);
                    _sb.AppendLine(msg);
                    Console.WriteLine(msg);
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