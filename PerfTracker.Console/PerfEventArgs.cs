using System;

namespace PerfTracker
{
    public class PerfEventArgs: EventArgs
    {
        public string Category { get; private set; }

        public string CounterName { get; private set; }

        public double Threashold { get; private set; }
        public double Count { get; private set; }

        public PerfEventArgs(string category, string name, double threashold, double count)
        {
            Category = category;
            CounterName = name;
            Threashold = threashold;
            Count = count;
        }
    }
}