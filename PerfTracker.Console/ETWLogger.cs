using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PerfTracker
{
    public class ETWLogger
    {
        static string _localIp = GetLocalIPAddress();
        static string _cmdArgsPattern = @"collect /LogFile=""{0}"" /BufferSizeMB=1000 /CircularMB=1000 /merge /zip /DotNetAlloc /DotNetAllocSampled /AcceptEULA {1} /MaxCollectSec:{2}";
        // /kernelEvents=default+Thread+ContextSwitch
        string _perfView, _etlPath, _logFile;
        int _maxCollectSec;

        public ETWLogger(string perfViewPath, string etlPath) : this(perfViewPath, etlPath, 600) { }

        public ETWLogger(string perfViewPath, string etlPath, int maxCollectSec)
        {
            _perfView = Path.Combine(perfViewPath, "perfview.exe");
            _etlPath = etlPath;
            _logFile = Path.Combine(_etlPath, string.Format("PerfViewCollect_{0}.log", _localIp));
            _maxCollectSec = maxCollectSec;
        }

        public void Log(object sender, PerfEventArgs e)
        {
            if (PerfviewIsRunning())
                return;

            ClearAppDataTemp();

            DateTime now = DateTime.Now;
            string fileToken = e.CounterName + "_" + e.Threashold.ToString() + "_" + now.ToString("yyyyMMddHH");
            foreach (var f in Directory.GetFiles(_etlPath, "*.zip"))
            {
                FileInfo fi = new FileInfo(f);
                if (fi.Name.IndexOf(fileToken) == 0)
                    return;
            }

            string etlfile = Path.Combine(_etlPath, string.Format("{0}{1}_{2}_{3}.etl", fileToken, now.ToString("mmss"), e.Count.ToString("N0"), _localIp));
            Process.Start(_perfView, string.Format(_cmdArgsPattern, _logFile, etlfile, _maxCollectSec.ToString()));
        }

        static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
            }
            return string.Empty;
        }

        static bool PerfviewIsRunning()
        {
            Process[] proc = Process.GetProcessesByName("perfview");
            return proc.Length > 0;
        }

        static void ClearAppDataTemp()
        {
            string temp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp");
            foreach (string dir in Directory.GetDirectories(temp, "perfview", SearchOption.AllDirectories))
            {
                Directory.Delete(dir, true);
            }
        }
    }
}
