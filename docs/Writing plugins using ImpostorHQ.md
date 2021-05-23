# Writing plugins using ImpostorHQ.

To write plugins using ImpostorHQ, inject the classes outlined in the `Dependencies` document.

Here is an example plugin requirement setup (taken from ImpostorHQ.Module.Banning)

```csharp
    [ImpostorPlugin("ihq.banning")]
    [ImpostorDependency("ihq.core", DependencyType.LoadBefore)]
    [ImpostorDependency("ihq.core", DependencyType.HardDependency)]
```

This will make your plugin depend on ImpostorHQ.

Now you can access the command handlers to create player/dashboard commands, assign HTTP endpoints for any API that you might want.

There is a test project, `ImpostorHQ.Module.Test`, so you can see how to use the command registers.

