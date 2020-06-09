using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EbParser
{
    static class Program
    {
        private static readonly string Separator = new string('=', 50);
        private const string SaveFilesArg = "-f";
        private const string StartPageArg = "-p=";
        private const string LogFile = "log.txt";
        private const string ErrorsFile = "error.txt";

        private static StringBuilder _sb;
        private static readonly object _lockObject = new object();

        private static void Main(string[] args)
        {
            try
            {
                MainAsync(args)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (Exception ex) 
            {
                Print($"Critical: { ex.Message }");
            }

            Console.ReadLine();
        }

        private static async Task MainAsync(string[] args)
        {
            _sb = new StringBuilder();
            var stopWatch = Stopwatch.StartNew();

            // Process command line arguments

            var saveFiles = args.Any(a => a == SaveFilesArg);
            var startPage = Array.Find(args, a => a.Contains(StartPageArg))?.Split("=").Last();
            int.TryParse(startPage, out int page);

            // Parse site

            using var parser = new Parser(saveFiles, page);
            parser.PageChangded += Worker_PageChangded;
            parser.Error += Worker_Error;
            parser.Report += Worker_Report;
            await parser.ParseAsync().ConfigureAwait(false);

            // Result

            File.WriteAllText(LogFile, _sb.ToString());
            _sb.Clear();
            Console.WriteLine($"Time: { stopWatch.Elapsed.TotalSeconds } sec");
            Console.WriteLine($"See log: { LogFile }");
            Console.WriteLine($"See errors: { ErrorsFile }");
        }

        #region Callbacks

        private static void Worker_Report(object sender, string message)
        {
            Print($"Info: { message }");
        }

        private static void Worker_Error(object sender, string message)
        {
            Print($"Error: { message }", ConsoleColor.Red);
            try
            {
                File.AppendAllText(ErrorsFile, message);
            }
            catch { }
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
            lock(_lockObject)
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