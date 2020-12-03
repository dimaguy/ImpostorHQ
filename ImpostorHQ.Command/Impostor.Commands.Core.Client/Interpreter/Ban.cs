using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Impostor.Commands.Core.Client.Interpreter
{
    public class Ban
    {
        /// <summary>
        /// The name of the banned player. Beware that it might be unknown, if the player was blindly banned.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The IP Address of the player.
        /// </summary>
        public IPAddress Address { get; set; }
    }
}
