# IDashboardCommandHandler dependency.

This class allows the registration of `DashboardCommand` objects via the `AddCommand` method. 



The `DashboardCommand` class is a pattern for commands sent from the dashboard. It's `prefix` can be anything. The `information` is used in the help command. See the `Command Parsing` document for information about the token count.

When a dashboard user calls the command and it is parsed successfully, the action supplied in the constructor is called with a `DashboardCommandNotification`, which contains the optional string parameters and the user that called the command.

