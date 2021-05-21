using System;
using System.Text.Json;

namespace ImpostorHQ.Module.Banning
{
    public struct PlayerBan
    {
        public string IpAddress { get; set; }

        public DateTime Time { get; set; }

        public string[] Witnesses { get; set; }

        public string PlayerName { get; set; }

        public PlayerBan(string ipAddress, string[] witnesses, DateTime time, string playerName)
        {
            IpAddress = ipAddress;
            Witnesses = witnesses;
            Time = time;
            PlayerName = playerName;
        }

        public string Serialize()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}