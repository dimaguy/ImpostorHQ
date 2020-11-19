using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;

namespace Impostor.Commands.Core.QuantumExtensionDirector
{
    public class PluginFileSystem
    {
        public readonly string Store,ConfigPath;
        public PluginFileSystem(string baseDirectory, string pluginName)
        {
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
    }
}
