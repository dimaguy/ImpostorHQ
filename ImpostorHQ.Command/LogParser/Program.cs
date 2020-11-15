using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Impostor.Commands.Core.SELF;

namespace LogParser
{
    class Program
    {
        private static long _logsRead = 0;
        private static StreamWriter _csvStream;
        static void Main(string[] args)
        {
            Console.WriteLine("ImpostorHQ .SELF log parser.");
            Console.Write("\nPath (drag and drop the file): ");
            var path = Console.ReadLine().Replace("\"", "").Replace("'", "");
            while (string.IsNullOrEmpty(path)||!File.Exists(path))
            {
                Console.Write("\nPlease input a valid path: ");
                path = Console.ReadLine().Replace("\"", "").Replace("'", "");
            }
            var destinationName = Path.GetFileNameWithoutExtension(path) + ".csv";
            var decoder = new SelfDecoder(File.ReadAllBytes(path));
            var t = new Thread(Status);
            t.Start();
            _csvStream = File.CreateText(destinationName);
            foreach (var bLog in decoder.ReadAll())
            {
                _logsRead++;
                Interpret(bLog);
            }
            _csvStream.Flush();
            _csvStream.Close();
            _logsRead = -1;
            Console.ReadLine();
        }

        private static void Status()
        {
            long read = 0;
            while ((read = _logsRead) != -1)
            {
                Console.Clear();
                Console.WriteLine($"Logs converted: {read}");
                Thread.Sleep(1000);
            }
            Console.Clear();
            Console.WriteLine("The logs have been successfully parsed.");
        }
        private static string CompileCsv(string[] data)
        {
            string rv = "";
            for (int i = 0; i < data.Length; i++)
            {
                rv += data[i];
                if (i != data.Length - 1)
                {
                    rv += ',';
                }
            }

            return rv;
        }
        private static void Interpret(SelfDecoder.BinaryLog bLog)
        {
            switch (bLog.Type)
            {
                case Shared.LogType.Dashboard:
                {
                    var dashboardLog = Shared.DashboardLog.Deserialize(bLog);
                    _csvStream.WriteLine(CompileCsv(new string[]
                    {
                        "Dashboard", "From " + dashboardLog.SourceIp, dashboardLog.Message
                    }));
                    break;
                }
                case Shared.LogType.Error:
                {
                    var errorLog = Shared.ErrorLog.Deserialize(bLog);
                    _csvStream.WriteLine(CompileCsv(new string[]
                    {
                        "Error", "Thrown in " + errorLog.Location, errorLog.Message
                    }));
                    break;
                }
            }
        }
    }
}
