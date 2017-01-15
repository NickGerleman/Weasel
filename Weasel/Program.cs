using System;
using Microsoft.Extensions.Logging;

namespace Wsl
{

    /// <summary>
    /// Entry point to the program
    /// </summary>
    public class Program
    {

        public static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Implement Me!");
            }
            catch (Exception ex)
            {
                Logger.Log("Unhandled Exception", LogLevel.Critical, ex);
                Environment.Exit(1);
            }
        }

    }

}
