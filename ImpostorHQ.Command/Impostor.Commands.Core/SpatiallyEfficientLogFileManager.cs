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
        private object FileLock = new object();
        public SpatiallyEfficientLogFileManager(string folderPath)
        {
            this.FolderPath = folderPath;
            Init();
            this.Encoder = new SelfEncoder((PreviousName = CompileName()));
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
    }
}
