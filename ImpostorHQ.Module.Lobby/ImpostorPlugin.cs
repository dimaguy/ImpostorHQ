using Impostor.Api.Plugins;

namespace ImpostorHQ.Module.Lobby
{
    [ImpostorPlugin("ihq.lobby")]
    [ImpostorDependency("ihq.core", DependencyType.LoadBefore)]
    [ImpostorDependency("ihq.core", DependencyType.HardDependency)]
    public class ImpostorPlugin : PluginBase
    {
        public ImpostorPlugin(LobbyCommands lobbyCommands)
        {

        }
    }
}
