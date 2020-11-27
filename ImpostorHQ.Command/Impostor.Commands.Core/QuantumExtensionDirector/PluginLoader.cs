using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using Microsoft.Extensions.Logging;

namespace Impostor.Commands.Core.QuantumExtensionDirector
{
    public class PluginLoader
    {
        #region Members
        public List<PluginInstance> Plugins { get; private set; }
        public Type TargetType { get; private set; }
        public uint ApiVersion { get; private set; }
        public string FolderPath { get; private set; }
        private string[] Stores { get; set; }
        public readonly QuiteExtendableDirectInterface Master;
        #endregion
        public PluginLoader(string folderPath, QuiteExtendableDirectInterface master, uint version)
        {
            this.ApiVersion = version;
            this.Plugins = new List<PluginInstance>();
            this.TargetType = typeof(IPlugin);
            this.FolderPath = folderPath;
            this.Master = master;
            AppDomain.CurrentDomain.AssemblyResolve += ResolveCrossPluginDependencies;
        }

        /// <summary>
        /// This is called when the server is starting.
        /// </summary>
        public void LoadPlugins()
        {
            //Fetch all types that implement the interface IPlugin and are a class
            foreach (var file in Directory.GetFiles(FolderPath))
            {
                if (file.EndsWith(".dll"))
                {
                    Assembly.LoadFile(Path.GetFullPath(file));
                }
            }
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var implemented = assemblies.SelectMany(a => a.GetTypes()).Where(p => TargetType.IsAssignableFrom(p) && p.IsClass).ToArray();

            for (int i = 0;i<implemented.Length;i++)
            {
                var type = implemented[i];
                var instance = ((IPlugin)Activator.CreateInstance(type));
                if (instance != null)
                {
                    if (instance.HqVersion != ApiVersion)
                    {
                        //I will still go ahead, but the user should know that errors may appear.
                        Master.UnsafeDirectReference.ConsolePluginWarning($"The plugin \"{instance.Name}\" is built on a different API version (current: {ApiVersion}, target: {instance.HqVersion}). This may induce unwanted behavior and/or errors.");
                        Master.UnsafeDirectReference.LogPlugin(instance.Name, $"Warning: The plugin is targeting version {instance.HqVersion}. Current API version: {ApiVersion}.");
                    }
                    Plugins.Add(new PluginInstance()
                    {
                        Main = type,
                        Instance = instance,
                    });
                    Master.UnsafeDirectReference.ConsolePluginStatus($"Loaded \"{instance.Name}\" by \"{instance.Author}\"");
                }
            }
            Stores = new string[Plugins.Count];
            
            for(int i = 0;i<Plugins.Count;i++)
            {
                var pfs = new PluginFileSystem(Path.Combine("hqplugins", "data"),Plugins[i].Instance.Name, this);
                Plugins[i].Instance.Load(Master, pfs);
                Stores[i] = pfs.Store;
            }
            Master.UnsafeDirectReference.ConsolePluginStatus($"Loaded {Plugins.Count} plugins.");
        }
        /// <summary>
        /// This resolves cross-plugin dependencies, so that plugin writer's don't have to get dirty.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private Assembly ResolveCrossPluginDependencies(object sender, ResolveEventArgs args)
        {
            if (args.Name.Contains(".resources")) return null;

            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
            if (assembly != null) return assembly;

            //how is it not loaded?
            //this is dangerous.
            //it is not a plugin, then.
            Master.Logger.LogWarning($"ImpostorHQ Plugin Loader : Could not implicitly resolve assembly \"{args.Name}\".");
            string filename = args.Name.Split(',')[0] + ".dll".ToLower();
            string asmFile = Path.Combine(@"..\", "hqplugins", filename);
            try
            {
                return Assembly.LoadFrom(asmFile);
            }
            catch
            {
                return null;
            }
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
                    if(plugin.Instance.Name.Contains(fullName)) return new CrossReferenceResult(plugin);
                }
            }
            return new CrossReferenceResult();
        }
        /// <summary>
        /// This is called when the server is shutting down.
        /// </summary>
        public void Shutdown()
        {
            foreach (var plugin in Plugins)
            {
                plugin.Instance.Destroy();
            }
        }
        /// <summary>
        /// Gets all active stores. It is equivalent to the function found in the PluginFileSystem class.
        /// </summary>
        /// <returns></returns>
        public string[] GetStores() => Stores;
        public class CrossReferenceResult
        {
            public CrossReferenceResult(PluginInstance instance)
            {
                this.Found = true;
                this.Instance = instance;
            }

            public CrossReferenceResult()
            {
                this.Found = false;
            }
            public bool Found { get; private set;}
            public PluginInstance Instance { get; private set; }
        }
        public struct PluginInstance
        {
            public Type Main { get; set; }
            public IPlugin Instance { get; set; }
        }
    }
}
