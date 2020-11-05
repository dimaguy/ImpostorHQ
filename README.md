# ImpostorHQ
ImpostorHQ is a plugin for the Impostor private Among Us server. It offers a way to monitor and administer the server remotely. It also provides an easy player chat interface for making command handlers.

## Usage
Once the plugin is installed, you must run the server once. A configuration file will be created. In that configuration file, you can change your API keys, add more, remove keys, and change the ports.
There are 3 parts to this plugin : the player chat command handler, which handles in-game commands, the web API server, which handles communication between dashboards and the server, and the HTTP server, which loads the client/dashboard onto browsers.

There also is an extension included, which handles bans. It is a good example of how to integrate with the API: sending commands to the dashboard, receiving them, overriding the command handler, and actually executing logic.
