using System;

namespace ImpostorHQ.Core.Util
{
    public class UnixDateProvider : IUnixDateProvider
    {
        public long GetEpoch() => (long) (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
    }

    public interface IUnixDateProvider
    {
        long GetEpoch();
    }
}