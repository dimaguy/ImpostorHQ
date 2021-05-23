using System;
using Impostor.Api.Plugins;

namespace ImpostorHQ.Module.Test
{
    [ImpostorPlugin("ihq.test")]
    [ImpostorDependency("ihq.core", DependencyType.LoadBefore)]
    [ImpostorDependency("ihq.core", DependencyType.HardDependency)]
    public class ImpostorPlugin : PluginBase
    {
        public ImpostorPlugin(PingPongHandler handler)
        {
            /*
            we requested the dependency so it gets constructed.
            for more information, read up on Dependency Injection,
            which is really important for Impostor and ImpostorHQ development. 
             */
        }
    }
}
