# ImpostorHQ Plugin System - Cross-plugin operations

The ImpostorHQ plugin system offers a very powerful way to share data between plugins. It allows for cross-plugin operations (accessing a plugin from another plugin). This opens up a lot of possibilities.

Note that the interop system is still early, and will change over time.

## 1. How does it work?

The system offers a method to search for the loaded plugins by their name. If the function finds the plugin, it will return the active instance, which you can cast to the respective object and work with it. 

In order to do that, one must add project reference to the plugin(s) they are targeting. You can then write code using references to those plugins.

A prime example of cross plugin operations is the `Hall of Shame` plugin. It accesses the `High Court` plugin in order to hook the events triggered when a player is banned or unbanned, and to read the permanent bans. You may refer to the source code for more information.

## 2. What does it offer?

The plugin loader offers a method to search for loaded plugins. That method returns the instance of the plugin, if loaded. It is expressed with the following syntax:

`reference.UnsafeDirectReference.PluginLoader.TryGetPlugin("the name of the plugin");`

Please note that using this feature can be unsafe. The plugin is not guaranteed to be installed, so one must handle both scenarios. _One must know what they are doing before they attempt to do cross-plugin operations._

The plugin loader itself will throw an exception if it is not able to load the dependencies.

## 3. Prerequisites.

Using the feature is easy. To get started, add a project reference to the plugin you wish to interface with. For this lesson, we will write a plugin that broadcasts messages when a player is permanently banned.

To do that, we will add a project reference to the ban handler.

![https://femto.pw/spiq](https://femto.pw/spiq)

We are ready to write our plugin!

## 4. Using the feature in our plugins.

First, make sure that the target plugin is installed. You may bundle it with your plugin. In our case, we need to have the  `High Court` plugin installed.

Now, we must first search for the loaded plugins.

`var result = reference.UnsafeDirectReference.PluginLoader.TryGetPlugin("High Court");`

After that, we can acquire a reference to the other plugin's main class.

`var justiceMain = (ImpostorHQ.Plugin.HighCourt.MainClass) result.Instance.MainClass;`

Congratulations! You are now ready to write logic that accesses the other plugin. 

The following is a full example:



```c#
public void Load(QuiteExtendableDirectInterface reference, PluginFileSystem system)
{
    var mainThread = new Thread((()=>Main(reference)));
    mainThread.Start();
}

private void Main(QuiteExtendableDirectInterface reference)
{
    Thread.Sleep(1000); //wait for all plugins to load before we access any of them.
    var result = reference.UnsafeDirectReference.PluginLoader.TryGetPlugin("High Court");
    var justiceMain = (ImpostorHQ.Plugin.HighCourt.MainClass)result.Instance.MainClass;
    justiceMain.HighCourt.OnPlayerBanned += (report) =>
    {
        foreach (var gameManagerGame in reference.GameManager.Games.ToList())
        {
            reference.ChatInterface.SafeMultiMessage(gameManagerGame, $"The player : {report.TargetName} has been permanently banned from this server!\nCheat, and you will end up here!", Structures.BroadcastType.Information);
        }
    };
}
```



![](https://raw.githubusercontent.com/dimaguy/ImpostorHQ/main/ImpostorHQ.Command/Plugin%20DOCS!/images/004-interop.gif)



