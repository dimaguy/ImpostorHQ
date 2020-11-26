using System;
using System.Net;
using System.Threading;
using System.Collections.Generic;

namespace Impostor.Commands.Core.DashBoard
{
    public class QuiteEffectiveDetector
    {
        // this super simple system will offer denial of service detection.
        // using this, we can effectively boot skids off.

        /// <summary>
        /// Will get or set the maximum packet rate allowed.
        /// </summary>
        public ushort RateThreshold { get; set; }

        private readonly List<Removable> ClearerQueue = new List<Removable>();
        private readonly List<Removable> UnBlockerQueue = new List<Removable>();
        private readonly List<IPAddress> Blocked = new List<IPAddress>();
        private readonly Dictionary<IPAddress,uint> History  = new Dictionary<IPAddress, uint>();
        public bool Running { get; private set; }
        public QuiteEffectiveDetector(ushort maxRequestsPerMinute)
        {
            this.Running = true;
            this.RateThreshold = (ushort)(maxRequestsPerMinute/6);
            var t = new Thread(UpdaterCallback);
            t.Start();
        }

        /// <summary>
        /// Use this whenever you receive a new connection. This will indicate whether or not the client is attacking you / the HTTP server / the API server.
        /// </summary>
        /// <param name="address">The address to check.</param>
        /// <returns>True if the address is attacking.</returns>
        public bool IsAttacking(IPAddress address)
        {
            lock (Blocked)
            {
                if (Blocked.Contains(address)) return true;
            }

            lock (History)
            {
                if (History.ContainsKey(address))
                {
                    if (History[address] > RateThreshold)
                    {
                        lock(Blocked) Blocked.Add(address);
                        lock(UnBlockerQueue) UnBlockerQueue.Add(new Removable(DateTime.Now, address));
                        History.Remove(address);
                        OnBlockedOnce?.Invoke(address);
                    }
                    else
                    {
                        History[address]++;
                    }
                }
                else
                {
                    History.Add(address,1);
                    lock(ClearerQueue) ClearerQueue.Add(new Removable(DateTime.Now, address));
                }
            }
            return false;
        }

        private void UpdaterCallback()
        {
            while (Running)
            {
                Thread.Sleep(1000);
                lock (ClearerQueue)
                {
                    for (int i = 0; i < ClearerQueue.Count; i++)
                    {
                        var item = ClearerQueue[i];
                        if ((DateTime.Now - item.StartTime).TotalSeconds < 10) continue;
                        lock (History)
                        {
                            if (History.ContainsKey(item.Address)) History.Remove(item.Address);
                        }

                        ClearerQueue.Remove(item);
                    }
                }

                lock (UnBlockerQueue)
                {
                    for (int i = 0; i < UnBlockerQueue.Count; i++)
                    {
                        var item = UnBlockerQueue[i];
                        if((DateTime.Now - item.StartTime).TotalSeconds < 300) continue;
                        UnBlockerQueue.Remove(item);
                        lock (Blocked)
                        {
                            if (Blocked.Contains(item.Address)) Blocked.Remove(item.Address);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Will return the current attackers.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<BlockedAddressInfo> GetBlocked()
        {
            lock (UnBlockerQueue)
            {
                if(UnBlockerQueue.Count==0) yield break;
                foreach (var removable in UnBlockerQueue)
                {
                    yield return new BlockedAddressInfo(removable.Address,removable.StartTime,DateTime.Now,(uint)(DateTime.Now - removable.StartTime).TotalSeconds);
                }
            }
        }

        public void Shutdown()
        {
            Running = false;
        }

        internal class Removable
        {
            public Removable(DateTime start, IPAddress address)
            {
                this.Address = address;
                this.StartTime = start;
            }
            public DateTime StartTime { get; set; }
            public IPAddress Address { get; set; }
        }

        public class BlockedAddressInfo
        {
            public BlockedAddressInfo(IPAddress addr, DateTime start, DateTime current, uint ago)
            {
                this.Address = addr;
                this.AttackStart = start;
                this.RecordDate = current;
                this.SecondsAgo = ago;
                this.SecondsLeft = 300 - ago;
            }
            /// <summary>
            /// The attacker's IPv4 Address.
            /// </summary>
            public IPAddress Address { get; private set; }
            /// <summary>
            /// The exact date when the address was blocked.
            /// </summary>
            public DateTime AttackStart { get; private set; }
            /// <summary>
            /// The time when this record was created.
            /// </summary>
            public DateTime RecordDate { get; private set; }
            /// <summary>
            /// The time since the address was blocked.
            /// </summary>
            public uint SecondsAgo { get; private set; }
            /// <summary>
            /// The time left until the address is unblocked.
            /// </summary>
            public uint SecondsLeft { get; private set; }
        }

        public delegate void DelBlockedFirst(IPAddress address);

        public event DelBlockedFirst OnBlockedOnce;
    }
}
