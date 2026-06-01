using System;
using System.Collections.Generic;
using System.IO;

namespace BlueLockScreen
{
    public static class Logger
    {
        private static readonly List<string> _entries = new List<string>();
        public static bool Enabled { get; set; }

        public static void Log(string message)
        {
            if (!Enabled) return;
            _entries.Add($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }

        public static void SaveToFile()
        {
            if (!Enabled || _entries.Count == 0) return;
            string fileName = $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            File.WriteAllLines(path, _entries);
            _entries.Clear();
        }
    }
}
