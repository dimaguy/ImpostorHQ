namespace ImpostorHQ.Core.Commands
{
    public interface ICommand
    {
        string Prefix { get; }

        string Information { get; }

        int Tokens { get; }
    }
}