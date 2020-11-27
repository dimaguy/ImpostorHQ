using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Impostor.Commands.Core.SELF
{
    class CsvComposer
    {
        private StringBuilder CsvStream { get; set; }
        private readonly object Lock = new object();
        public CsvComposer()
        {
            CsvStream = new StringBuilder();
        }
        /// <summary>
        /// This will convert SELF binary data to a human-readable CSV log.
        /// </summary>
        /// <param name="selfLog">The SELF binary data.</param>
        /// <returns>A CSV string formatted with UTF8, using CRLF.</returns>
        public string Compose(byte[] selfLog)
        {
            lock (Lock)
            {
                CsvStream.Clear();
                using (var decoder = new SelfDecoder(selfLog))
                {
                    foreach (var log in decoder.ReadAll())
                    {
                        Interpret(log);
                    }
                }

                return CsvStream.ToString();
            }
        }
        /// <summary>
        /// This is used to compile CSV strings.
        /// </summary>
        /// <param name="data">The cells to write.</param>
        /// <returns>A CSV string. Beware that it does not contain line breaks.</returns>
        private string CompileCsv(string[] data)
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
        /// <summary>
        /// This is used to interpret a SELF log, and convert it to rich, human-readable CSV.
        /// </summary>
        /// <param name="bLog">The binary log.</param>
        private void Interpret(SelfDecoder.BinaryLog bLog)
        {
            switch (bLog.Type)
            {
                case Shared.LogType.Dashboard:
                {
                    var dashboardLog = Shared.DashboardLog.Deserialize(bLog);
                    CsvStream.AppendLine(CompileCsv(new string[]
                    {
                        bLog.TimeStamp.ToString("T"),"Dashboard", "From " + dashboardLog.SourceIp, dashboardLog.Message
                    }));
                    break;
                }
                case Shared.LogType.Error:
                {
                    var errorLog = Shared.ErrorLog.Deserialize(bLog);
                    CsvStream.AppendLine(CompileCsv(new string[]
                    {
                        bLog.TimeStamp.ToString("T"),"Error", "Thrown in " + errorLog.Location, errorLog.Message
                    }));
                    break;
                }
                case Shared.LogType.Plugin:
                {
                    var pluginLog = Shared.PluginLog.Deserialize(bLog);
                    CsvStream.AppendLine(CompileCsv(new string[]
                    {
                        bLog.TimeStamp.ToString("T"),$"Plugin {pluginLog.PluginName}", pluginLog.Message
                    }));
                    break;
                }
            }
        }
    }
}
