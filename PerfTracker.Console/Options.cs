using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace PerfTracker
{
    public class Options
    {
        [Option('p', "processId", Required = true, HelpText = ".NET process id")]
        public int ProcessId { get; set; }

        [Option('v', "perfviewLocation", Required = true, HelpText = "The local loaction path of Perfview.exe")]
        public string PerfviewLocation { get; set; }

        [Option('e', "etlLocation", Required = true, HelpText = "ETL file output loaction")]
        public string EtlLocation { get; set; }

        [Option('i', "interval", DefaultValue = 30, HelpText = "Interval seconds to collect performance counters.")]
        public int Interval { get; set; }

        [Option('c', "cpu", DefaultValue = 50, HelpText = "Cpu usage threashold.")]
        public int Cpu { get; set; }

        [Option('m', "memory", DefaultValue = 60, HelpText = "Memory usage threashold.")]
        public int UsedMemoryPercent { get; set; }

        [Option('l', "loh", DefaultValue = 7, HelpText = ".NET Loh usage threashold.")]
        public int Loh { get; set; }

        [Option('o', "echo", DefaultValue = true, HelpText = "Print details to screen.")]
        public bool Echo { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            // this without using CommandLine.Text
            //  or using HelpText.AutoBuild
            var usage = new StringBuilder();
            usage.AppendLine("usage: perftracker -p processId -v perfview_loation -e output_location \t\n\t[-i interval_seconds] [-c cpu_usage] [-m memory_usage] [-l loh_usage]");
            usage.AppendLine("\t\n");
            usage.AppendLine("optional arguments:");
            usage.AppendLine("-i\tInterval seconds to collect performance counters, default is 30s.");
            usage.AppendLine("-c\tCpu usage percent threashold,default is 50%.");
            usage.AppendLine("-m\tMemory usage percent threashold,default is 60%.");
            usage.AppendLine("-l\tLoh usage percent threashold,default is 7%.");

            return usage.ToString();
        }
    }
}
