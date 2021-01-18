# ImpostorHQ
[![Build Status](https://dev.azure.com/dimahq/ImpostorHQ/_apis/build/status/dimaguy.ImpostorHQ?branchName=main)](https://dev.azure.com/dimahq/ImpostorHQ/_build/latest?definitionId=1&branchName=main) [Demo Page](https://dimaguy.github.io/ImpostorHQ/)  
ImpostorHQ is a plugin for [Impostor](https://github.com/Impostor/Impostor) (Among Us Private Server Implementation).  
It offers a way to monitor and manage the server remotely. It does also provide a Command System component for both admins (in a web dashboard) and players (In-Lobby commands).  

This software is extensible through the usage of plugins, we provide a very permissive api that allows to access everything that we provide in addition to impostor API itself.

## Installation
1. Go on [Releases](https://github.com/dimaguy/ImpostorHQ/releases)
2. Download the latest one
3. Unzip it in impostor's root folder (the one where the impostor binary is in)
4. Port forward 22023/tcp(WS API) and 22024/both (http server and AU announcements)
5. Enjoy :)  

## Notes for docker installation  
Make sure you leave mount points for:  
`/app/configs` -  ImpostorHQ Configs  
`/app/hqplugins` - ImpostorHQ Plugins  
`/app/dashboard` - Dashboard Files  

## Usage
Once the plugin is installed, you must run the server once. An API key(that you can use for login) and a configuration file will be created. In that configuration file, you can change your API keys, add more, remove keys, and change the ports.
