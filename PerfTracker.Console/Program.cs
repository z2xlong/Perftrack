using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfTracker
{
    class Program
    {
        static TextWriter _out = Console.Out;
        static TextReader _in = Console.In;

        static int _pid, _i = 60, _cpu = 50, _amem = 20, _loh = 15;
        static bool _echo;
        static string _perfviewPath;
        static string _etlPath;

        static int Main()
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;

            if (!InitArgs(Environment.GetCommandLineArgs()))
            {
                WriteHelp();
                return -1;
            }

            ETWLogger logger = new ETWLogger(_perfviewPath, _etlPath);
            Tracker tracker = new Tracker(_pid, _i, _cpu, _amem, _loh, _echo);
            tracker.PerfOverThreshold += logger.Log;
            tracker.CollectCounters();
            return 0;
        }

        private static bool InitArgs(string[] args)
        {
            ConsoleArgs ca = new ConsoleArgs(args);

            if (!int.TryParse(ca["p"], out _pid))
            {
                _out.WriteLine("Invalid process id format, must be integer number.");
                return false;
            }
            if (!string.IsNullOrWhiteSpace(ca["cpu"]) && !int.TryParse(ca["cpu"], out _cpu))
            {
                _out.WriteLine("Invalid cpu percent format, must be integer number between 0 to 100.");
                return false;
            }
            if (!string.IsNullOrWhiteSpace(ca["amem"]) && !int.TryParse(ca["amem"], out _amem))
            {
                _out.WriteLine("Invalid available memory percent format, must be integer number between 0 to 100.");
                return false;
            }
            if (!string.IsNullOrWhiteSpace(ca["loh"]) && !int.TryParse(ca["loh"], out _loh))
            {
                _out.WriteLine("Invalid available LOH percent format, must be integer number between 0 to 100.");
                return false;
            }
            if (!string.IsNullOrWhiteSpace(ca["i"]) && !int.TryParse(ca["i"], out _i))
            {
                _out.WriteLine("Invalid checking performance counter interval seconds, must be integer between 0 to 100.");
                return false;
            }


            _perfviewPath = ca["v"];
            if (!File.Exists(Path.Combine(_perfviewPath.TrimEnd('\\'), "perfview.exe")))
            {
                _out.WriteLine("PerfView exe file could not be found.");
                return false;
            }

            _etlPath = ca["e"];
            if (!Directory.Exists(_etlPath))
            {
                _out.WriteLine("ETL file target path is not exist.");
                return false;
            }

            _echo = ca.ExistArg("o");

            return true;
        }

        private static void WriteHelp()
        {
            _out.WriteLine(@"Args invalid! Please follow the sample: perftracker -p 14052 -v ""d:\perfview"" -e ""d:\dump""");
        }

        private static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            _out.WriteLine(e.ExceptionObject.ToString());
        }
    }
}
