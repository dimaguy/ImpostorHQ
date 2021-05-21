using System.Text;
using Impostor.Api.Plugins;
using ImpostorHQ.Http;
using ImpostorHQ.Http.Handler;
using ImpostorHQ.Module.Banning;

namespace ImpostorHQ.Module.HallOfShame
{
    [ImpostorPlugin("ihq.shame")]
    [ImpostorDependency("ihq.banning", DependencyType.LoadBefore)]
    [ImpostorDependency("ihq.banning", DependencyType.HardDependency)]
    public class ImpostorPlugin : PluginBase
    {
        public ImpostorPlugin(HtmlGenerator generator, HttpServer server)
        {
            server.AddHandler(new DynamicHandler("/shame", () => ("text/html", Encoding.UTF8.GetBytes(generator.Generate()))));
        }
    }
}
