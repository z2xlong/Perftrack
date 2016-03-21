using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualBasic.Devices;

namespace PerfTracker
{
    public class Tracker
    {
        readonly int _sleepIntMs, _cpu, _amem, _loh;
        Process _process;
        readonly string _processName, _userName;
        readonly bool _echo;
        readonly ulong _totalMbMems;

        public EventHandler<PerfEventArgs> PerfOverThreshold;

        public Tracker(int pId, int intSec, int cpu, int amem, int loh, bool echo = false)
        {
            _totalMbMems = getPhysicalMemory() / 1024 / 1024;

            _process = Process.GetProcessById(pId);
            _processName = _process.ProcessName;
            _userName = ProcessUtility.GetProcessOwner(pId);

            _sleepIntMs = intSec * 1000;
            _cpu = cpu;
            _amem = amem;
            _loh = loh;
            _echo = echo;
        }

        public void CollectCounters()
        {
            int failed = 0;
            PerformanceCounter memCounter = new PerformanceCounter("Memory", "Available MBytes"); ;
            PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            PerformanceCounter loh = GetLohPerfCounter();

            while (true)
            {
                if (_process != null && !_process.HasExited)
                {
                    try
                    {
#if DEBUG
                        Console.WriteLine("Process id : {0}, Process Name : {1}, User Name: {2}", _process.Id, _processName, _userName);
#endif
                        DetectLoh(loh.NextValue());
                        failed = 0;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Tracking is broken since {0} : {1}", ex.GetType().ToString(), ex.Message);
                    }
                }
                else if (failed < 10)
                {
                    failed += 1;

#if DEBUG
                    Console.WriteLine("Original Process has exited, retry {0} times.", failed);
#endif

                    if (ProcessUtility.TryGetProcess(_processName, _userName, out _process))
                        loh = GetLohPerfCounter();
                }
                else
                {
                    Console.WriteLine("Process {0} with user name {1} has exited.", _processName, _userName);
                    break;
                }

                DetectCPU(cpuCounter.NextValue());
                DetectMem(memCounter.NextValue());

                Thread.Sleep(_sleepIntMs);
            }
        }

        void DetectCPU(double cpuPercent)
        {
            var cp = cpuPercent;

            if (_echo)
                Console.WriteLine(string.Format("CPU: {0}", cp.ToString("N0")));

            for (int i = 90; i >= _cpu; i -= 10)
            {
                if (cp >= i)
                {
                    FirePerf("Process", "CPU", i, cp);
                    break;
                }
            }
        }

        void DetectLoh(double lohBytes)
        {
            var lohPercent = Math.Round((lohBytes / 1024 / 1024 / _totalMbMems) * 100, 2);

            if (_echo)
                Console.WriteLine(string.Format("LOH Percent: {0}", lohPercent.ToString()));

            for (int i = 40; i >= _loh; i -= 5)
            {
                if (lohPercent >= i)
                {
                    FirePerf("CLR", "LohPercent", i, lohPercent);
                    break;
                }
            }
        }

        void DetectMem(double memMB)
        {
            var memPercent = Math.Round((memMB / _totalMbMems) * 100, 2);

            if (_echo)
                Console.WriteLine(string.Format("Available Memory Percent: {0}", memPercent.ToString()));

            for (int i = _amem; i >= 0; i -= 10)
            {
                if (memPercent <= i)
                {
                    FirePerf("Process", "AvaMemPercent", i, memPercent);
                    break;
                }
            }
        }

        void FirePerf(string category, string name, double threashold, double count)
        {
            EventHandler<PerfEventArgs> handler = PerfOverThreshold;
            if (handler != null)
            {
                handler(this, new PerfEventArgs(category, name, threashold, count));
            }
        }

        ulong getPhysicalMemory()
        {
            var computer = new ComputerInfo();
            return computer.TotalPhysicalMemory;
        }

        PerformanceCounter GetLohPerfCounter()
        {
            string instance = ProcessUtility.GetManagedPerformanceCounterInstanceName(_process);
#if DEBUG
            Console.WriteLine("Performance Counter Instance name is {0}", instance);
#endif
            return new PerformanceCounter(".NET CLR Memory", "Large Object Heap size", instance);
        }
    }
}
