using System;
using System.Diagnostics;
using Microsoft.VisualBasic.Devices;

namespace PerfTracker
{
    public class Tracker
    {
        Process _process;
        readonly Options _opt;
        readonly string _processName, _userName;
        readonly ulong _totalMbMems;
        double _lastUsedMem;
        PerformanceCounter avMemCounter, cpuCounter, loh;

        public EventHandler<PerfEventArgs> PerfOverThreshold;

        public Tracker(Options opt)
        {
            _opt = opt;

            _process = Process.GetProcessById(_opt.ProcessId);
            _processName = _process.ProcessName;
            _userName = ProcessUtility.GetProcessOwner(_opt.ProcessId);

            avMemCounter = new PerformanceCounter("Memory", "Available MBytes"); ;
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

            _totalMbMems = getPhysicalMemory() / 1024 / 1024;
        }

        public bool CollectCounters()
        {
            DetectMemory(avMemCounter.NextValue());
            DetectEvent("Process", "CPU", _opt.Cpu, cpuCounter.NextValue());

            if (_process != null && !_process.HasExited)
            {
                try
                {
                    if (loh == null)
                        loh = GetLohPerfCounter();

                    DetectEvent("CLR", "LohPercent", _opt.Loh, (loh.NextValue() / 1024 / 1024 / _totalMbMems) * 100);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("Tracking is broken since {0} : {1}", ex.GetType().ToString(), ex.Message);
                    return false;
                }
            }

            return true;
        }

        void DetectMemory(double avaliableMem)
        {
            var usedmem = _totalMbMems - avaliableMem;

            if (_lastUsedMem > 0)
                DetectEvent("Process", "UsedMemDropPercent", 10, (_lastUsedMem / usedmem - 1) * 100);
            DetectEvent("Process", "UsedMemPercent", _opt.UsedMemoryPercent, (usedmem / _totalMbMems) * 100);

            _lastUsedMem = usedmem;
        }

        void DetectEvent(string category, string name, double threashold, double perf)
        {
            if (_opt.Echo)
                Console.WriteLine(string.Format("{0}: {1}", name, perf.ToString("N0")));

            if (perf >= threashold)
            {
                EventHandler<PerfEventArgs> handler = PerfOverThreshold;
                if (handler != null)
                    handler(this, new PerfEventArgs(category, name, threashold, perf));
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
            return new PerformanceCounter(".NET CLR Memory", "Large Object Heap size", instance);
        }
    }
}
