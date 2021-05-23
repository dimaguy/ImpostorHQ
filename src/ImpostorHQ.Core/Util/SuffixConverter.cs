using System;

namespace ImpostorHQ.Core.Util
{
    public static class SuffixConverter
    {
        private static readonly string[] Suffixes = new string[] {"bytes", "KB", "MB", "GB"};

        public static string Convert(long value, ushort accuracy = 2)
        {
            if (value == 0)
            {
                return "0 bytes";
            }

            var magnitude = (int) Math.Log(value, 1024);

            var size = (decimal) value / (1L << (magnitude * 10));

            if (Math.Round(size, accuracy) >= 1000)
            {
                magnitude += 1;
                size /= 1024;
            }

            return string.Format("{0:n" + accuracy + "} {1}", size, Suffixes[magnitude]);
        }

        public static string ToSizeNotation(this long value)
        {
            return Convert(value);
        }
    }
}