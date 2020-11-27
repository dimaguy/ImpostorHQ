using System.IO;
using System.Text.Json;

namespace Impostor.Commands.Core.QuantumExtensionDirector
{
    public class PluginFileSystem
    {
        public readonly string Store,ConfigPath;
        private readonly PluginLoader loader;
        public PluginFileSystem(string baseDirectory, string pluginName,PluginLoader loader)
        {
            this.loader = loader;
            Store = Path.Combine(baseDirectory, pluginName);
            ConfigPath = Path.Combine(Store, $"{pluginName}.cfg");
            if (!Directory.Exists(Store)) Directory.CreateDirectory(Store);
        }
        /// <summary>
        /// This will check if this is the first time a config file is being accessed.
        /// </summary>
        /// <returns>True if there is no prior config file.</returns>
        public bool IsDefault()
        {
            return !File.Exists(ConfigPath);
        }
        /// <summary>
        /// This is used to load a config. It must exist on the disk.
        /// </summary>
        /// <typeparam name="T">Your serializable config class. Just use variables with getters and setters to store your data.</typeparam>
        /// <returns></returns>
        public T ReadConfig<T>()
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(ConfigPath));
        }
        /// <summary>
        /// This is used to save your config file.
        /// </summary>
        /// <typeparam name="T">Your config object's type.</typeparam>
        /// <param name="config">Your config data.</param>
        public void Save<T>(T config)
        {
            if(File.Exists(ConfigPath))File.Delete(ConfigPath);
            File.WriteAllText(ConfigPath,JsonSerializer.Serialize<T>(config));
        }
        /// <summary>
        /// This is used to get all active plugin folders.
        /// </summary>
        /// <returns></returns>
        public string[] GetAllStores()
        {
            return loader.GetStores();
        }
    }
}
