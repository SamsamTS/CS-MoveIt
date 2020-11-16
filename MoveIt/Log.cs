using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Linq;
using UnityEngine;

namespace MoveIt
{
    // Adapted from KianCommons by Kian.Zarrin
    public class Log
    {
        private static string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        private static readonly Stopwatch Timer;
        static string nl = Environment.NewLine;

        /// <summary>
        /// File name for log
        /// </summary>
        private static readonly string LogFile = Path.Combine(Application.dataPath, assemblyName + ".log");

        /// <summary>
        /// Log levels. Also output in log file.
        /// </summary>
        private enum LogLevel
        {
            Debug,
            Info,
            Error,
        }

        /// <summary>
        /// Initializes static members of the <see cref="Log"/> class.
        /// Resets log file on startup.
        /// </summary>
        static Log()
        {
            try
            {
                if (File.Exists(LogFile))
                {
                    File.Delete(LogFile);
                }

                Timer = Stopwatch.StartNew();

                AssemblyName details = typeof(Log).Assembly.GetName();
                Info($"{details.Name} v{details.Version.ToString()}", true);
            }
            catch
            {
                // ignore
            }
        }

        /// <summary>
        /// Logs debug trace
        /// </summary>
        /// <param name="message">Log entry text.</param>
        /// <param name="copyToGameLog">If <c>true</c> will copy to the main game log file.</param>
        public static void Debug(string message, bool copyToGameLog = true)
        {
            LogImpl(message, LogLevel.Debug, copyToGameLog);
        }

        /// <summary>
        /// Logs info message.
        /// </summary>
        /// 
        /// <param name="message">Log entry text.</param>
        /// <param name="copyToGameLog">If <c>true</c> will copy to the main game log file.</param>
        public static void Info(string message, bool copyToGameLog = false)
        {
            LogImpl(message, LogLevel.Info, copyToGameLog);
        }

        /// <summary>
        /// Logs error message and also outputs a stack trace.
        /// </summary>
        /// 
        /// <param name="message">Log entry text.</param>
        /// <param name="copyToGameLog">If <c>true</c> will copy to the main game log file.</param>
        public static void Error(string message, bool copyToGameLog = true)
        {
            LogImpl(message, LogLevel.Error, copyToGameLog);

        }

        /// <summary>
        /// Write a message to log file.
        /// </summary>
        /// 
        /// <param name="message">Log entry text.</param>
        /// <param name="level">Logging level. If set to <see cref="LogLevel.Error"/> a stack trace will be appended.</param>
        private static void LogImpl(string message, LogLevel level, bool copyToGameLog)
        {
            try
            {
                var ticks = Timer.ElapsedTicks;
                string msg = "";
                
                int maxLen = Enum.GetNames(typeof(LogLevel)).Select(str => str.Length).Max();
                msg += string.Format($"{{0, -{maxLen}}}", $"[{level}] ");

                long secs = ticks / Stopwatch.Frequency;
                long fraction = ticks % Stopwatch.Frequency;
                msg += string.Format($"{secs.ToString("n0")}.{fraction.ToString("D7")} | ");

                msg += message + nl;

                if (level == LogLevel.Error)
                {
                    msg += new StackTrace(true).ToString() + nl + nl;
                }

                using (StreamWriter w = File.AppendText(LogFile))
                {
                    w.Write(msg);
                }

                if (copyToGameLog)
                {
                    msg = assemblyName + " | " + msg;
                    switch (level)
                    {
                        case LogLevel.Error:
                            UnityEngine.Debug.LogError(msg);
                            break;
                        default:
                            UnityEngine.Debug.Log(msg);
                            break;
                    }
                }
            }
            catch
            {
                // ignore
            }
        }
    }
}

