using System;
using Impostor.Api.Plugins;

namespace ImpostorHQ.Module.Banning
{
    [ImpostorPlugin("ihq.banning")]
    [ImpostorDependency("ihq.core", DependencyType.LoadBefore)]
    [ImpostorDependency("ihq.core", DependencyType.HardDependency)]
    public class ImpostorPlugin : PluginBase
    {
        public ImpostorPlugin(BanManager manager) { }
    }
}
