using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Impostor.Commands.Core.QuantumExtensionDirector
{
    public class PluginLoader
    {
        public List<IPlugin> Plugins { get; private set; }
        public Type TargetType { get; private set; }
        public uint ApiVersion { get; private set; }
        public string FolderPath { get; private set; }
        public readonly QuiteExtendableDirectInterface Master;
        public PluginLoader(string folderPath, QuiteExtendableDirectInterface master, uint version)
        {
            this.ApiVersion = version;
            this.Plugins = new List<IPlugin>();
            this.TargetType = typeof(IPlugin);
            this.FolderPath = folderPath;
            this.Master = master;
        }

        public void LoadPlugins()
        {
            foreach (var file in Directory.GetFiles(FolderPath))
            {
                if (file.EndsWith(".dll"))
                {
                    Assembly.LoadFile(Path.GetFullPath(file));
                }
            }

            //Fetch all types that implement the interface IPlugin and are a class
            var implemented = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(p => TargetType.IsAssignableFrom(p) && p.IsClass)
                .ToArray();
            foreach (Type type in implemented)
            {
                var x = ((IPlugin)Activator.CreateInstance(type));
                if (x != null)
                {
                    if (x.HqVersion != ApiVersion)
                    {
                        Master.UnsafeDirectReference.ConsolePluginWarning($"The plugin \"{x.Name}\" is built on a different API version (current: {ApiVersion}, target: {x.HqVersion}). This may induce unwanted behavior and/or errors.");
                        //I will still go ahead, but the user should know that errors may appear.
                        Master.UnsafeDirectReference.LogPlugin(x.Name, $"Warning: The plugin is targeting version {x.HqVersion}. Current API version: {ApiVersion}.");
                    }
                    Plugins.Add(x);
                    Master.UnsafeDirectReference.ConsolePluginStatus($"Loaded \"{x.Name}\" by \"{x.Author}\"");
                }
            }
            foreach (var plugin in Plugins)
            {
                plugin.Load(Master, new PluginFileSystem(Path.Combine("hqplugins", "data"), plugin.Name));
            }
            Master.UnsafeDirectReference.ConsolePluginStatus($"Loaded {Plugins.Count} plugins.");
        }

        public void Shutdown()
        {
            foreach (var plugin in Plugins)
            {
                plugin.Destroy();
            }
        }
    }
}
