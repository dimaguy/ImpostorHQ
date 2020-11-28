using System;
using System.IO;
using Impostor.Commands.Core;
using Impostor.Commands.Core.QuantumExtensionDirector;

namespace ImpostorHQ.Plugin.DiskStatus
{
    public class MainClass : IPlugin
    {
        public string Name => "Disk status";

        public string Author => "anti";

        public uint HqVersion => 4;

        public QuiteExtendableDirectInterface PluginBase { get; private set; }
        public PluginFileSystem FileSys { get; private set; }
        public const string MainCommand = "/diskstatus";
        public void Destroy()
        {
        }

        public void Load(QuiteExtendableDirectInterface reference, PluginFileSystem system)
        {
            this.PluginBase = reference;
            this.FileSys = system;
            Main();
        }

        public void Main()
        {
            PluginBase.ApiServer.RegisterCommand(MainCommand,"=> displays storage information.");
            PluginBase.OnDashboardCommandReceived += (command, data, single,source) =>
            {
                if (command.Equals(MainCommand))
                {
                    long size = 0;
                    long current = 0;
                    string result = "Sizes:\n";

                    current = (long)SizeOf("hqplugins");
                    size += current;
                    result += $"    Plugins total: {Suffix.Convert(current,2)}\n";

                    current = (long)SizeOf("hqlogs");
                    size += current;
                    result += $"    Logs total: {Suffix.Convert(current, 2)}\n";

                    current = (long)SizeOf("configs");
                    size += current;
                    result += $"    Configs total: {Suffix.Convert(current, 2)}\n";

                    current = (long)SizeOf("dashboard");
                    size += current;
                    result += $"    Website total: {Suffix.Convert(current, 2)}\n";

                    current = (long)SizeOf("libraries");
                    size += current;
                    result += $"    Libs total: {Suffix.Convert(current, 2)}\n";

                    result += $"    Listed total: {Suffix.Convert(size, 2)}";
                    PluginBase.ApiServer.PushTo(result,"(diskinfo)",Structures.MessageFlag.ConsoleLogMessage,source);
                }
            };
        }

        private ulong SizeOf(string directory)
        {
            var dirInfo = new DirectoryInfo(directory);
            ulong size = 0;
            foreach(var file in dirInfo.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                if (file.Length <= 0) continue;
                size += (ulong)file.Length;
            }

            return size;
        }
    }
    class Suffix
    {
        private static readonly string[] SizeSuffixes =
            { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        public static string Convert(Int64 value, int decimalPlaces = 1)
        {
            if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
            if (value < 0) { return "-" + Convert(-value); }
            if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                SizeSuffixes[mag]);
        }
    }
}
