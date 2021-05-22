# Command Parsing

Command parsing is carried out by the `ICommandParser<T>` class. The operation of this class is dictated by the supplied `ICommand` implementation.

## Command Interface

The `ICommand` interface holds a prefix, information and min/max token count. It is implemented by 2 types of commands by default (the dashboard and the player commands). 

### Simple Commands

If both token counts are 0, the expected behavior is one that triggers your invoker when the prefix is supplied as the input string for the parser.

If the string contains no spaces, the parser will try to find the command, looking up the string supplied for parsing. If it is found, and the token count matches (zero), the invoker is called. Else, an `UnknownCommand` status is emitted, in case the command prefix was not found, or a `NoData` status, if the command requires data. 

### Commands with arguments

If the token count is above 0, the parser will expect arguments separated by spaces.

If the string contains spaces, it is split, and the first token is looked up as the prefix. If found, and the token count matches with the found command, the invoker is called. Else, an `UnknownCommand` status is emitted, in case the command prefix was not found, or an `InvalidSyntax` status in case the token count required does not match the min/max count from splitting the string - 1.

Note: The parser tokenizes the input string and ignores regions inside quotes.

### Examples

Command with no tokens: `?`, `/help`, ...

Command with tokens: `/ban Dimaguy` - this command has one token, which will  be "Dimaguy".