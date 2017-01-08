using System;
using Microsoft.Extensions.Logging;

namespace Rs
{

    /// <summary>
    /// Global state shared between the generic loggers
    /// </summary>
    internal static class LogState
    {
        /// <summary>
        /// Whether all loggers are enabled
        /// </summary>
        public static bool Enabled { get; set; } = true;
    }


    /// <summary>
    /// Program-wide Logger
    /// </summary>
    public class Logger : LoggerFor<Program> {}


    /// <summary>
    /// Logger for a specific class
    /// </summary>
    public class LoggerFor<T>
    {
        private static ILogger<T> instance;


        /// <summary>
        /// Enable or disable all loggers
        /// </summary>
        /// <param name="isEnabled">Whether loggers should be enabled</param>
        public static void SetLoggingEnabled(bool isEnabled)
        {
            LogState.Enabled = isEnabled;
        }


        /// <summary>
        /// Log a message
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="level">The importance of the event</param>
        /// <param name="ex">A related exception</param>
        public static void Log(string message, LogLevel level = LogLevel.Information, Exception ex = null)
        {
            if (!LogState.Enabled)
                return;

            // Formatter is the same as the abstraction built into Logger
            GetInstance().Log(level, 0 /*eventId*/, message, ex, (state, e) => state.ToString()/*formatter*/);
        }


        /// <summary>
        /// Get an instance of the logger
        /// </summary>
        private static ILogger<T> GetInstance()
        {
            if (instance == null)
            {
                #if (DEBUG)
                instance = new LoggerFactory()
                            .AddConsole()
                            .AddDebug()
                            .CreateLogger<T>();
                #else
                // TODO Add log file
                instance = new LoggerFactory()
                            .AddConsole()
                            .CreateLogger<T>();
                #endif
            }

            return instance;
        }
    }

}

