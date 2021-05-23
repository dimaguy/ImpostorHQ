using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImpostorHQ.Module.Lobby
{
    public class Config
    {
        public const string Section = "IHQL";

        public byte MaxPlayers { get; set; } = 10;

        public byte MaxImpostors { get; set; } = 3;
    }
}
