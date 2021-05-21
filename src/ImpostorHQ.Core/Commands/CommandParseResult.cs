#nullable enable
namespace ImpostorHQ.Core.Commands
{
    public readonly struct CommandParseResult<T> where T : class, ICommand
    {
        public T? Command { get; }

        public string[]? Tokens { get; }

        public ParseStatus Error { get; }

        public CommandParseResult(T command, string[] tokens, ParseStatus reason = ParseStatus.None)
        {
            Error = reason;
            Command = command;
            Tokens = tokens;
        }
    }

    public enum ParseStatus
    {
        None,
        Unspecified,
        UnknownCommand,
        NoData,
        InvalidSyntax,
        WhiteSpace
    }
}