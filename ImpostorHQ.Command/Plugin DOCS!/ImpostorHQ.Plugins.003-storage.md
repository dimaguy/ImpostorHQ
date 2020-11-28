# ImpostorHQ Plugin System - Standard file storage

The ImpostorHQ plugin API offers a standard way of storing configs or files. The plugin's initial method passes an object called `PluginFileSystem`. That object offers a path to store your files, and easy functions to save and load your configs. Using it is most trivial, and can be done as follows:

`system.IsDefault()` -> returns true if there is no config file on the disk. You can use this to create a config the first time your plugins is used, or to load it if there already is a config file on the disk.

`system.ConfigPath` -> returns the standard config path that was automatically generated for your plugin.

`system.Store` -> returns the standard folder, where you can store files associated with your plugin.

`system.Save(configObject)` -> will save your config object to the standard config path.

`system.ReadConfig<T>()` -> will read the config. Be careful, the function has no checks to see if there is a config on the disk. You must handle that yourself.

The following is a fully functional example:

```c#
public void Load(QuiteExtendableDirectInterface reference, PluginFileSystem system)
{
    MyConfig config;
    if (system.IsDefault())
    {
        //this is the first time our plugin is being used.
        config = MyConfig.GetDefault();
        system.Save(config);
    }
    else
    {
        config = system.ReadConfig<MyConfig>();
    }
    reference.Logger.LogInformation($"{config.Greeting}");
}

class MyConfig
{
    public string Greeting { get; set; }

    public static MyConfig GetDefault()
    {
        return new MyConfig(){Greeting = "Hi there!"};
    }
}
```



The first time we run this code, we get the following message:

![https://femto.pw/e62j](https://femto.pw/e62j)

You will notice that a folder and config file has been created for our plugin:

![https://femto.pw/a75h](https://femto.pw/a75h)

We can now freely edit the config, in order to display any greeting we want, when the server starts.

![https://femto.pw/dp2u](https://femto.pw/dp2u)

That's all. See you next time!

`-anti`