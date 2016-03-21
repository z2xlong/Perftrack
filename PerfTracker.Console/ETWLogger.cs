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
        //DateTime _recentCall = DateTime.MinValue;
        static string _localIp = GetLocalIPAddress();
        static string _cmdArgsPattern = @"collect /LogFile=""{0}"" /BufferSizeMB=500 /CircularMB=1000 /merge /zip /DotNetAlloc /DotNetAllocSampled /MaxCollectSec:300 /AcceptEULA {1}";

        string _perfView, _etlPath, _logFile;

        public ETWLogger(string perfViewPath, string etlPath)
        {
            _perfView = Path.Combine(perfViewPath, "perfview.exe");
            _etlPath = etlPath;
            _logFile = Path.Combine(_etlPath, string.Format("PerfViewCollect_{0}.log", _localIp));
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Log(object sender, PerfEventArgs e)
        {
            if (PerfviewIsRunning())
                return;
            //if (_recentCall.AddMinutes(10) > DateTime.Now)
            //return;

            //_recentCall = DateTime.Now;

            string fileToken = e.CounterName + "_" + e.Threashold.ToString();
            foreach (var f in Directory.GetFiles(_etlPath, "*.zip"))
            {
                FileInfo fi = new FileInfo(f);
                if (fi.Name.IndexOf(fileToken) == 0)
                    return;
            }

            string etlfile = Path.Combine(_etlPath, string.Format("{0}_{1}_{2}_{3}.etl", fileToken, e.Count.ToString(), DateTime.Now.ToString("yyyyMMddHHmmss"), _localIp));
            Process.Start(_perfView, string.Format(_cmdArgsPattern, _logFile, etlfile));
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
    }
}
