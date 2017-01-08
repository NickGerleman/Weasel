using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Rs.Util.Assert
{

    /// <summary>
    /// Assertion Utilities
    /// </summary>
    public static class Assert
    {

        /// <summary>
        /// Assert a condition to be true only when debugging
        /// </summary>
        /// <param name="condition">The condition to check</param>
        /// <param name="msg">An error message</param>
        [Conditional("DEBUG")]
        public static void Debug(bool condition, string msg)
        {
            System.Diagnostics.Debug.Assert(condition, msg);
        }


        /// <summary>
        /// Assert a condition to be true
        /// </summary>
        /// <param name="condition">The condition to check/param>
        /// <param name="msg">An error message</param>
        /// <returns>Whwther the condition is true</returns>
        public static bool That(bool condition, string msg)
        {
            var message = $"Assertion Failed: \"{msg}\"\n{Environment.StackTrace}";
            Logger.Log(message, LogLevel.Error);
            return condition;
        }

    }

}
