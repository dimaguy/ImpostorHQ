using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualBasic;

namespace ImpostorHQ.Module.Banning
{
    public readonly struct PlayerBan
    {
        public string IpAddress{ get; }

        public DateTime Time { get; }

        public string[] Witnesses{ get; }

        public string PlayerName { get; }

        public PlayerBan(string address, string[] witnesses, DateTime time, string playerName)
        {
            this.IpAddress = address;
            this.Witnesses = witnesses;
            this.Time = time;
            this.PlayerName = playerName;
        }

        public string Serialize()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
