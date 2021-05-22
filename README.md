# ImpostorHQ

[back end plugin rewritten by Alioth Merak]

ImpostorHQ is an [Impostor](https://github.com/Impostor/Impostor) plugin for executing commands remotely, from a web client and lobbies. It has a small HTTP server, uses Fleck for the API, and has a primitive encryption scheme utilizing XTEA. 

Functionality is added by other Impostor plugins, such as the ban handler, which adds banning, or the Hall of Shame, which adds HTTP pages for viewing the bans.

To install it, grab the latest release, and drop the `plugins` and `libraries` folder into your Impostor server's directory. Passwords are stored in `IHQ_Passwords.txt` as username:password. By default, the client is located at `/ihq`. Enter a username:password combination in the login field, and click login. Use /help to get an idea of the functionality you acquired.

A configuration file can be created, to change the API port, HTTP port, dashboard end point and host interface.

###### Note that running this behind a proxy is recommended. One may even choose to ditch the HTTP server entirely, and host the files on a good server like NGINX or Apache.

# The Ban Handler

The included ban system is very useful for administration. One can ban an IP address, and manage the database of bans. It has commands for adding, removing, listing, and viewing bans. The ban database is also exported on `bans.csv?apikey`.

The bans are stored in a file called `ImpostorHQ.Bans.jsondb`. Each line contains a JSON record.