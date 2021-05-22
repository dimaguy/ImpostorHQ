# IPlayerCommandHandler dependency.

This class allows the registration of `PlayerCommand` objects via the `AddCommand` method. 



The `PlayerCommand` class is a pattern for commands sent from from players via the in-game chat. It's `prefix` must start with a forward slash, and must not contain any spaces. The `information` is used in the help command. See the `Command Parsing` document for information about the token count.

When a player calls the command and it is parsed successfully, the action supplied in the constructor is called with a `PlayerCommandNotification`, which contains the optional string parameters and the IClientPlayer that called the command.

