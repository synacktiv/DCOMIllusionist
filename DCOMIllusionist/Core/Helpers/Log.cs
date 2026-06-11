using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCOMIllusionist.Core.Helpers
{
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warn = 2,
        Error = 3,
        Fatal = 4,
        Success = 5
    }
    public static class Log
    {
        public static LogLevel MinimumLevel { get; set; } = LogLevel.Info;
        public static void Debug(string message) => LogMessage(LogLevel.Debug, "*", message);
        public static void Info(string message) => LogMessage(LogLevel.Info, "+", message);
        public static void Warn(string message) => LogMessage(LogLevel.Warn, "!", message);
        public static void Error(string message) => LogMessage(LogLevel.Error, "x", message);
        public static void Success(string message) => LogMessage(LogLevel.Success, "+", message);
        private static void LogMessage(LogLevel level, string symbol, string message)
        {
            if (level < MinimumLevel) return;
            Console.WriteLine($"[{symbol}] {message}");
        }
    }
}
