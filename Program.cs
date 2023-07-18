using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WampTest1
{
    public class Program
    {
        public static string OutputFolderPath = @"C:\ProgramData\Bio-Rad\WampTest\";
        public static string LogFileName = "WampTest.log";
        public static string LogFile;
        public static StreamWriter LogFileStream;
        public static WampInterface WampIF;
        static void Main(string[] args)
        {
            LogFile = Path.Combine(OutputFolderPath, LogFileName);
            Directory.CreateDirectory(OutputFolderPath);

            if (!File.Exists(LogFile))
                File.Create(Path.Combine(OutputFolderPath, LogFileName));
            LogFileStream = new StreamWriter(Path.Combine(OutputFolderPath, LogFileName), true);
            
            ReportAndLog($"================================================================");
            ReportAndLog($"===         New Session Starting                             ===");
            
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            ConnectWamp();
            WaitForUserExit();
        }

        public static async Task ConnectWamp()
        {
            WampIF = new WampInterface();

            await WampIF.ConnectToLocalServer();
            // await WampIF.ConnectToRemoteServer();
        }


        private static void WaitForUserExit()
        {
            string input;
            while (true)
            {
                Console.WriteLine("Enter a command.  Enter \"ex\" to quit:");
                input = Console.ReadLine();
                if (input.Contains("exit") || input.Contains("stop") || input == "ex")
                    break;
            }
        }
        /// <summary>
        /// Capture Application.ThreadException and AppDomain.Unhandled Exception events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnUnhandledException(Object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                ReportAndLog(ex.Message);
            }
            else
            {
                // couldn't get an exception object, create a default one
                ReportAndLog(new Exception("Unknown exception thrown.").Message);
            }
        }

        public static void ReportAndLog(string message)
        {
            Console.WriteLine(message);
            LogFileStream.WriteLine(message);
        }
    }
}
