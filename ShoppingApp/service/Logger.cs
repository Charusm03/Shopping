using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShoppingApplication.Service
{
    public static class Logger
    {
        private static readonly string _file = Path.Combine(AppContext.BaseDirectory, "Logs", "app.log");
        private static readonly Queue<string> _recent = new Queue<string>();
        private static readonly object _lock = new Object();

        static Logger()
        {
            var dir = Path.GetDirectoryName(_file);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        public static void Info(string msg) => Write("INFO", msg, true);
        public static void Warn(string msg) => Write("WARN", msg, true);
        public static void Error(string msg) => Write("ERROR", msg, true);

        public static string[] RecentLog()
        {
            lock (_lock)
            {
                return _recent.ToArray();
            }
        }

        private static void Write(string level, string msg, bool showInPanel)
        {
            lock (_lock)
            {
                File.AppendAllText(_file, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {msg}{Environment.NewLine}");
                if (!showInPanel) return;
                _recent.Enqueue($"[{level}] {msg}");
                while (_recent.Count > 5) _recent.Dequeue();
            }
        }
    }
}
