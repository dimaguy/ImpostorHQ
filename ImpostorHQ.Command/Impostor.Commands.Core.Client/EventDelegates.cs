using System;
using System.Collections.Generic;
using System.Text;
using Impostor.Commands.Core.Client.Interpreter;

namespace Impostor.Commands.Core.Client.Networking
{
    public class EventDelegates
    {
        public delegate void DelStatusReceived(StatusUpdate data);

        public delegate void DelTextReceived(string txt);

        public delegate void DelBansListed(List<Ban> bans);

        public delegate void DelLogsListed(List<string> files);
    }
}
