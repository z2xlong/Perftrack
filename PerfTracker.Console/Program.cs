using System;
using CommandLine;
using System.Threading;

namespace PerfTracker
{
    class Program
    {
        static int Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;

            var opts = new Options();
            if (Parser.Default.ParseArguments(args, opts))
            {
                ETWLogger logger = new ETWLogger(opts.PerfviewLocation, opts.EtlLocation);
                Tracker tracker = new Tracker(opts);
                tracker.PerfOverThreshold += logger.Log;

                int failed = 0;
                while (true)
                {
                    failed = tracker.CollectCounters() ? 0 : failed + 1;

                    if (failed < 10)
                        Thread.Sleep(opts.Interval * 1000);
                    else
                    {
                        Console.WriteLine("Retry {0} times failed.", failed.ToString());
                        return -1;
                    }
                }
            }
            else
            {
                Console.WriteLine(opts.GetUsage());
                return -1;
            }
        }


        private static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.ExceptionObject.ToString());
        }
    }
}
