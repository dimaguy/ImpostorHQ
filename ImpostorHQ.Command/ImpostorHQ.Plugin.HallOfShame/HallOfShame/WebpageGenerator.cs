using System.IO;
using Impostor.Commands.Core;
using System.Collections.Generic;
using ImpostorHQ.Plugin.HighCourt;

namespace ImpostorHQ.Plugin.HallOfShame.HallOfShame
{
    public class WebpageGenerator
    {
        public SegmentedHtml Html { get; private set; }
        private string RecordBase { get; set; }
        public WebpageGenerator(string folderPath)
        {
            this.RecordBase = File.ReadAllText(Path.Combine(folderPath, "record.partial"));
            this.Html = new SegmentedHtml(
                File.ReadAllText(Path.Combine(folderPath,"start.partial")), 
                File.ReadAllText(Path.Combine(folderPath, "end.partial")), RecordBase.Replace("%username%","Hurray! There are no specific player bans."));
        }

        /// <summary>
        /// Adds a player to the hall of shame.
        /// </summary>
        /// <param name="report"></param>
        public void AddBan(JusticeSystem.Report report)
        {
            var rec = GenerateRecord(report);
            if (string.IsNullOrEmpty(rec)) return;
            Html.AddRecord(rec);
        }
        /// <summary>
        /// Removes a player from the hall of shame.
        /// </summary>
        /// <param name="report"></param>
        public void RemoveReport(JusticeSystem.Report report)
        {
            var rec = GenerateRecord(report);
            if (string.IsNullOrEmpty(rec)) return;
            Html.RemoveRecord(rec);
        }

        private string GenerateRecord(JusticeSystem.Report rep)
        {
            if (rep.TargetName.Equals("<unknown>")) return null;
           return RecordBase.Replace("%username%", rep.TargetName.Replace("<", "").Replace(">", ""));
        }
    }

    public class SegmentedHtml
    {
        public string StartOfHtml { get; set; }
        private List<string> Records { get; set; }
        public string EndOfHtml { get; set; }
        private string Html { get; set; }
        private bool changed = true;
        private readonly object locker = new object();
        private readonly string Initial;
        public SegmentedHtml(string start, string end, string initialRecord)
        {
            this.StartOfHtml = start;
            this.EndOfHtml = end;
            this.Records = new List<string>();
            this.Initial = initialRecord;
            Records.Add(initialRecord);
            GetLatest();
        }

        public void AddRecord(string data)
        {
            lock (locker)
            {
                changed = true;
                if (!Records.Contains(data))
                {
                    Records.Add(data);
                    if(Records.Contains(Initial)) Records.Remove(Initial);
                }
            }
        }

        public void RemoveRecord(string record)
        {
            lock (locker)
            {
                changed = true;
                if (Records.Contains(record))
                {
                    Records.Remove(record);
                    if(Records.Count == 0) Records.Add(Initial);
                }
            }
        }

        public string GetLatest()
        {
            if (changed)
            {
                lock (locker)
                {
                    this.Html = string.Empty;
                    this.Html = StartOfHtml;
                    foreach (var record in Records)
                    {
                        this.Html += record;
                    }

                    this.Html += EndOfHtml;
                    changed = false;
                }
            }

            return this.Html;
        }
    }
}
