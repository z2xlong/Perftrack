using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfTracker
{
    public class ConsoleArgs
    {
        Dictionary<string, string> _argDic;

        public string this[string key]
        {
            get
            {
                string value;
                return _argDic.TryGetValue(key, out value) ? value : null;
            }
        }

        public bool ExistArg(string key)
        {
            return _argDic.ContainsKey(key);
        }

        public ConsoleArgs(string[] args)
        {
            ParseArgs(args);
        }

        void ParseArgs(string[] args)
        {
            Stack<string> keys = new Stack<string>();
            _argDic = new Dictionary<string, string>();

            foreach (string arg in args)
            {
                if (IsToken(arg))
                    keys.Push(arg.Substring(1));
                else if (keys.Count > 0)
                    _argDic[keys.Pop()] = arg;
            }

            while (keys.Count > 0)
                _argDic.Add(keys.Pop(), null);
        }

        private bool IsToken(string arg)
        {
            if (arg[0] != '-' && arg[0] != '/')
                return false;

            return !string.IsNullOrWhiteSpace(arg);
        }

    }
}
