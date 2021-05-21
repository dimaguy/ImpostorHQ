using ImpostorHQ.Core.Commands;

namespace ImpostorHQ.Tests.Mocks
{
    class MockCommand : ICommand
    {
        public MockCommand(string prefix, string information, int tokens)
        {
            Prefix = prefix;
            Information = information;
            Tokens = tokens;
        }

        public string Prefix { get; }

        public string Information { get; }

        public int Tokens { get; }
    }
}