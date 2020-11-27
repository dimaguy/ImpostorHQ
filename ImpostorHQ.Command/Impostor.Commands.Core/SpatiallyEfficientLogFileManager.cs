using System;
using System.IO;
using System.Net;
using Impostor.Commands.Core.SELF;
namespace Impostor.Commands.Core
{
    public class SpatiallyEfficientLogFileManager
    {
        public string FolderPath { get; set; }
        private string PreviousName { get; set; }
        private SelfEncoder Encoder { get; set; }
        public readonly object FileLock = new object();
        public SpatiallyEfficientLogFileManager(string folderPath)
        {
            this.FolderPath = folderPath;
            Init();
            this.PreviousName = CompileName();
            this.Encoder = new SelfEncoder(PreviousName);
        }
        private void Init()
        {
            if (!Directory.Exists(FolderPath)) Directory.CreateDirectory(FolderPath);
        }
        private void HandleEncoder()
        {
            if (!PreviousName.Equals(CompileName()))
            {
                Encoder.End();
                PreviousName = CompileName();
                Encoder.Start(PreviousName);
            }
        }
        private string CompileName()
        {
            return Path.Combine(FolderPath,DateTime.Now.ToString("yyyy-MM-dd") + ".self");
        }

        /// <summary>
        /// This is used to log dashboard commands.
        /// </summary>
        /// <param name="ipa">The IP address of the client.</param>
        /// <param name="command">The command to log.</param>
        public void LogDashboard(IPAddress ipa,string command)
        {
            lock (FileLock)
            {
                HandleEncoder();
                Encoder.WriteDashboardLog(ipa,command);
            }
        }
        /// <summary>
        /// This is used to log data from plugins.
        /// </summary>
        /// <param name="source">The name of the plugin. Can also be an identifier to help locate the source of the message.</param>
        /// <param name="message">The message.</param>
        public void LogPlugin(string source, string message)
        {
            lock (FileLock)
            {
                HandleEncoder();
                Encoder.WritePluginLog(source,message);
            }
        }
        /// <summary>
        /// This is used to log exceptions. You may use this from anywhere.
        /// </summary>
        /// <param name="trace">The error message.</param>
        /// <param name="source">The source of the message. This can also be a location within a plugin.</param>
        public void LogError(string trace, Shared.ErrorLocation source)
        {
            lock (FileLock)
            {
                HandleEncoder();
                Encoder.WriteExceptionLog(trace,source);
            }
        }
        /// <summary>
        /// This is used to signal the end of the session. Do not use this!
        /// </summary>
        public void Finish()
        {
            Encoder.End();
        }
        /// <summary>
        /// This will get all the log file paths.
        /// </summary>
        /// <returns></returns>
        public string[] GetLogNames()
        {
            return Directory.GetFiles(FolderPath);
        }
    }
}
