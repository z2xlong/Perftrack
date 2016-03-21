using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Globalization;
using System.Runtime.Remoting.Messaging;
using System.Management;

namespace PerfTracker
{
    public class ProcessUtility
    {
        const string CategoryNetClrMemory = ".NET CLR Memory";
        const string ProcessId = "Process ID";
        const int ProcessesToTry = 40;


        public static string GetInstanceNameForProcess(int instanceCount, Process p)
        {
            string instanceName = Path.GetFileNameWithoutExtension(p.MainModule.FileName);

            if (instanceCount > 0) // Append instance counter
            {
                instanceName += "#" + instanceCount.ToString();
            }

            // Reader .NET CLR Memory Process ID for the given instance to check if
            // it does match our target process
            using (PerformanceCounter counter = new PerformanceCounter(CategoryNetClrMemory, ProcessId,
                   instanceName, true))
            {

                long id = 0;

                try
                {
                    while (true)
                    {
                        var sample = counter.NextSample();
                        id = sample.RawValue;

                        // for some reason it takes quite a while until the counter is
                        // updated with the correct data
                        if (id > 0)
                            break;

                        Thread.Sleep(15);
                    }
                }
                catch (InvalidOperationException)
                {
                    // swallow exceptions from non existing instances we tried to read
                }

                return (id == p.Id) ? instanceName : null;
            }

        }

        public static string GetManagedPerformanceCounterInstanceName(Process p)
        {
            Func<int, Process, string> PidReader = GetInstanceNameForProcess;
            string instanceName = null;
            AutoResetEvent ev = new AutoResetEvent(false);

            for (int i = 0; i < ProcessesToTry; i++)
            {
                int tmp = i;
                // Since reading the performance counter for every process is
                // very slow we try to speed up our search by reading up to ProcessesToTry
                // in parallel
                PidReader.BeginInvoke(tmp, p, (IAsyncResult res) =>
                {
                    if (instanceName == null)
                    {
                        string correctInstanceName = PidReader.EndInvoke(res);

                        if (correctInstanceName != null)
                        {
                            instanceName = correctInstanceName;
                            ev.Set();
                        }
                    }

                }, null);
            }


            // wait until we got the correct instance name or give up
            if (!ev.WaitOne(20 * 1000))
            {
                throw new InvalidOperationException("Could not get managed performance counter instance name for process " + p.Id);
            }

            return instanceName;
        }

        public static string GetProcessOwner(int processId)
        {
            string query = "Select * From Win32_Process Where ProcessID = " + processId;
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();

            foreach (ManagementObject obj in processList)
            {
                string[] argList = new string[] { string.Empty, string.Empty };
                int returnVal = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
                if (returnVal == 0)
                {
                    return argList[1] + "\\" + argList[0];   // return DOMAIN\user
                }
            }
            return "NO OWNER";
        }

        public static bool TryGetProcess(string processName, string userName, out Process process)
        {
            process = null;
#if DEBUG
            Console.WriteLine("Try get process by process name : {0}, user name : {1}", processName, userName);
#endif
            try
            {
                string query = "Select * From Win32_Process Where Name = '" + processName + ".exe'";
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
                ManagementObjectCollection processList = searcher.Get();

                foreach (ManagementObject obj in processList)
                {
                    string[] argList = new string[] { string.Empty, string.Empty };
                    int returnVal = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
                    if (returnVal == 0)
                    {
                        var un = argList[1] + "\\" + argList[0];
#if DEBUG
                        Console.WriteLine("ManagementObject user name : {0}, process id : {1}", un, obj["ProcessId"]);
#endif
                        if (userName == un)
                        {
                            process = Process.GetProcessById(int.Parse(obj["ProcessId"].ToString()));
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return false;
        }
    }
}
