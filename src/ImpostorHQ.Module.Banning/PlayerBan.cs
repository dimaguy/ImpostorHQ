using System;
using System.Text.Json;

namespace ImpostorHQ.Module.Banning
{
    public struct PlayerBan
    {
        public string IpAddress { get; set; }

        public DateTime Time { get; set; }

        public string[] Witnesses { get; set; }

        public string[] PlayerNames { get; set; }

        public string Reason { get; set; }

        public PlayerBan(string ipAddress, string[] witnesses, DateTime time, string[] playerNames, string reason)
        {
            IpAddress = ipAddress;
            Witnesses = witnesses;
            Time = time;
            PlayerNames = playerNames;
            this.Reason = reason;
        }

        public string Serialize()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}