using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImpostorHQ.Core.Commands;

namespace ImpostorHQ.Tests.Mocks
{
    class MockCommand : ICommand
    {
        public string Prefix { get; }

        public string Information { get; }

        public int Tokens { get; }

        public MockCommand(string prefix, string information, int tokens)
        {
            Prefix = prefix;
            Information = information;
            Tokens = tokens;
        }
    }
}
