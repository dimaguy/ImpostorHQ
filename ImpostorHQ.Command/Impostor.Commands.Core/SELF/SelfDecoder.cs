using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Impostor.Commands.Core.SELF
{

    public class SelfDecoder:IDisposable
    {
        public MemoryStream IOStream { get; private set; }
        public SelfDecoder(byte[] selfBytes)
        {
            IOStream = new MemoryStream(selfBytes);
        }
        /// <summary>
        /// This is used to read a log from the physical stream.
        /// </summary>
        /// <returns>A deserialized binary log.</returns>
        public BinaryLog ReadLog()
        {
            var sizeBytes = new byte[2];
            IOStream.Read(sizeBytes, 0, 2);
            var size = BitConverter.ToUInt16(sizeBytes, 0);
            var data = new byte[size];
            IOStream.Read(data, 0, size);
            return BinaryLog.Deserialize(new MemoryStream(data), size);
        }
        /// <summary>
        /// This is used to read all logs from the physical device.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<BinaryLog> ReadAll()
        {
            while (IOStream.Position != IOStream.Length) yield return ReadLog();
        }
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public class BinaryLog
        {
            public ushort BaseLength { get; set; }
            public Shared.LogType Type { get; set; }
            public DateTime TimeStamp { get; set; }
            public byte[] LogData { get; set; }
            public static BinaryLog Deserialize(MemoryStream stream, ushort baseLength)
            {
                var type = stream.ReadByte();
                var buffer = new byte[8];
                stream.Read(buffer, 0, 8);
                var epoch = BitConverter.ToUInt64(buffer, 0);
                buffer = new byte[baseLength - 9];
                stream.Read(buffer, 0, buffer.Length);
                return new BinaryLog
                {
                    BaseLength = baseLength,
                    Type = (Shared.LogType)type,
                    TimeStamp = GetTime(epoch),
                    LogData = buffer
                };
            }

            private static DateTime GetTime(ulong unixTime)
            {
                return Epoch.AddMilliseconds(unixTime);
            }
        }
        public void Dispose()
        {
            IOStream.Dispose();
        }
    }
}
