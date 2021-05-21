using System;

namespace ImpostorHQ.Core.Util
{
    public class UnixDateProvider
    {
        public long GetEpoch() => (long) (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
    }
}