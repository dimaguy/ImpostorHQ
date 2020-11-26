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

        public bool IsDefault()
        {
            return !File.Exists(ConfigPath);
        }

        public T ReadConfig<T>()
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(ConfigPath));
        }

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
