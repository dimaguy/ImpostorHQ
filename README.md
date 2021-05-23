# ImpostorHQ
[![Build Status](https://dev.azure.com/dimahq/ImpostorHQ/_apis/build/status/dimaguy.ImpostorHQ?branchName=main)](https://dev.azure.com/dimahq/ImpostorHQ/_build/latest?definitionId=1&branchName=main) [Demo Page](https://dimaguy.github.io/ImpostorHQ/)  
ImpostorHQ is a plugin for [Impostor](https://github.com/Impostor/Impostor) (Among Us Private Server Implementation).  
It offers a way to monitor and manage the server remotely. It does also provide a Command System component for both admins (in a web dashboard) and players (In-Lobby commands).  

It has a small HTTP server, uses Fleck for the API, and has a primitive encryption scheme utilizing BlackTEA (A fork of XTEA). 

Functionality can be added by other Impostor plugins, such as the ban handler, which adds banning, or the Hall of Shame, which adds HTTP pages for viewing the bans.

## Installation
To install it, grab the latest release, and drop the `plugins` and `libraries` folder into your Impostor server's directory. Passwords are stored in `IHQ_Passwords.txt` as username:password. By default, the client is located at `[IP/domain]/ihq`. Enter a username:password combination in the login field, and click login. Use /help to get an idea of the functionality you acquired.

A configuration file can be created, to change the API port, HTTP port, dashboard end point and host interface.

###### Note that running this behind a proxy is recommended. One may even choose to ditch the HTTP server entirely, and host the files on a good server like NGINX or Apache.

## Notes for docker installation  
Make sure you leave mount points for:  
`/app/configs` -  ImpostorHQ Configs  
`/app/ImpostorHQ.Overrides` - *Optional* You can use this folder to override a default dashboard file with your own. We provide a zip folder in each release with the default resources to modify  

# The Ban Handler

The included ban system is very useful for administration. One can ban an IP address, and manage the database of bans. It has commands for adding, removing, listing, and viewing bans. The ban database is also exported on `bans.csv?apikey`.

The bans are stored in a file called `ImpostorHQ.Bans.jsondb`. Each line contains a JSON record.
