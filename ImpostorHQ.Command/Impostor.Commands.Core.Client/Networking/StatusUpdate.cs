using System;
using System.Collections.Generic;
using System.Text;

namespace Impostor.Commands.Core.Client.Networking
{
    public class StatusUpdate
    {
        public uint MemoryUsage { get; private set; }
        public uint CpuUsage { get; private set; }
        public uint PlayerCount { get; private set; }
        public uint LobbyCount { get; private set; }
        public uint TotalServerUpTimeMinutes { get; private set; }
        public StatusUpdate(uint memory, uint cpu, uint players, uint lobbies, uint uptime)
        {
            this.MemoryUsage = memory;
            this.CpuUsage = cpu;
            this.PlayerCount = players;
            this.LobbyCount = lobbies;
            this.TotalServerUpTimeMinutes = uptime;
        }
    }
}
