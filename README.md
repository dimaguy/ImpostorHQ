# ImpostorHQ
[![Build Status](https://dev.azure.com/dimahq/ImpostorHQ/_apis/build/status/dimaguy.ImpostorHQ?branchName=main)](https://dev.azure.com/dimahq/ImpostorHQ/_build/latest?definitionId=1&branchName=main)  
ImpostorHQ is a plugin for [Impostor](https://github.com/Impostor/Impostor) (Among Us Private Server Implementation).  
It offers a way to monitor and manage the server remotely. It does also provide a Command System component for both admins (in a web dashboard) and players (In-Lobby commands).  

This software is extensible through the usage of plugins, we provide a very permissive api that allows to access everything that we provide in addition to impostor API itself.

## Usage
Once the plugin is installed, you must run the server once. A configuration file will be created. In that configuration file, you can change your API keys, add more, remove keys, and change the ports.
There are 3 parts to this plugin : the player chat command handler, which handles in-game commands, the web API server, which handles communication between dashboards and the server, and the HTTP server, which loads the client/dashboard onto browsers.

There also is an extension included, which handles bans. It is a good example of how to integrate with the API: sending commands to the dashboard, receiving them, overriding the command handler, and actually executing logic.
