using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Net;
using System.Reflection.Metadata;
using System.Text;
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

        public void LogDashboard(IPAddress ipa,string command)
        {
            lock (FileLock)
            {
                HandleEncoder();
                Encoder.WriteDashboardLog(ipa,command);
            }
        }

        public void LogPlugin(string source, string message)
        {
            lock (FileLock)
            {
                HandleEncoder();
                Encoder.WritePluginLog(source,message);
            }
        }

        public void LogError(string trace, Shared.ErrorLocation source)
        {
            lock (FileLock)
            {
                HandleEncoder();
                Encoder.WriteExceptionLog(trace,source);
            }
        }

        public void Finish()
        {
            Encoder.End();
        }

        public string[] GetLogNames()
        {
            return Directory.GetFiles(FolderPath);
        }
    }
}
