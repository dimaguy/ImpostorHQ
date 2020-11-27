using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace Impostor.Commands.Core.QuantumExtensionDirector
{
    public class PluginLoader
    {
        #region Members
        public List<IPlugin> Plugins { get; private set; }
        public Type TargetType { get; private set; }
        public uint ApiVersion { get; private set; }
        public string FolderPath { get; private set; }
        private string[] Stores { get; set; }
        public readonly QuiteExtendableDirectInterface Master;
        #endregion
        public PluginLoader(string folderPath, QuiteExtendableDirectInterface master, uint version)
        {
            this.ApiVersion = version;
            this.Plugins = new List<IPlugin>();
            this.TargetType = typeof(IPlugin);
            this.FolderPath = folderPath;
            this.Master = master;
        }
        /// <summary>
        /// This is called when the server is starting.
        /// </summary>
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
            Stores = new string[Plugins.Count];
            int index = 0;
            foreach (var plugin in Plugins)
            {
                var pfs = new PluginFileSystem(Path.Combine("hqplugins", "data"),plugin.Name, this);
                plugin.Load(Master, pfs);
                Stores[index] = pfs.Store;
            }
            Master.UnsafeDirectReference.ConsolePluginStatus($"Loaded {Plugins.Count} plugins.");
        }

        /// <summary>
        /// Will try to get a reference to another ImpostorHQ plugin.
        /// </summary>
        /// <param name="fullName">The full name of the plugin.</param>
        /// <returns>An object representing the results of the search.</returns>
        public CrossReferenceResult TryGetPlugin(string fullName)
        {
            lock (Plugins)
            {
                foreach (var plugin in Plugins)
                {
                    if(plugin.Name.Equals(fullName)) return new CrossReferenceResult(plugin);
                }
            }
            return new CrossReferenceResult(null);
        }
        /// <summary>
        /// This is called when the server is shutting down.
        /// </summary>
        public void Shutdown()
        {
            foreach (var plugin in Plugins)
            {
                plugin.Destroy();
            }
        }
        /// <summary>
        /// Gets all active stores. It is equivalent to the function found in the PluginFileSystem class.
        /// </summary>
        /// <returns></returns>
        public string[] GetStores() => Stores;
        public class CrossReferenceResult
        {
            public CrossReferenceResult(IPlugin plugin)
            {
                if (plugin == null) this.Found = false;
                else
                {
                    this.Found = true;
                    this.Plugin = plugin;
                }
            }
            public bool Found { get; private set;}
            public IPlugin Plugin { get; private set; }
        }
    }
}
