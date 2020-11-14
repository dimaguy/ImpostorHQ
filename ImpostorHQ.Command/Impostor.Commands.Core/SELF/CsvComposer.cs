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
        private void Interpret(SelfDecoder.BinaryLog bLog)
        {
            switch (bLog.Type)
            {
                case Shared.LogType.Dashboard:
                {
                    var dashboardLog = Shared.DashboardLog.Deserialize(bLog);
                    CsvStream.AppendLine(CompileCsv(new string[]
                    {
                        "Dashboard", "From " + dashboardLog.SourceIp, dashboardLog.Message
                    }));
                    break;
                }
                case Shared.LogType.Error:
                {
                    var errorLog = Shared.ErrorLog.Deserialize(bLog);
                    CsvStream.AppendLine(CompileCsv(new string[]
                    {
                        "Error", "Thrown in " + errorLog.Location, errorLog.Message
                    }));
                    break;
                }
            }
        }
    }
}
