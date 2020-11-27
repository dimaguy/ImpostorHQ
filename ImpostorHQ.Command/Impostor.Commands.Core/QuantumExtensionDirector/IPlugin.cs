using System;

namespace Impostor.Commands.Core.QuantumExtensionDirector
{
    public interface IPlugin
    {
        string Name { get; }
        string Author { get; }
        UInt32 HqVersion { get; }
        /// <summary>
        /// This is called when the plugin is being loaded.
        /// </summary>
        /// <param name="reference">"The dog's unmentionables'.</param>
        /// <param name="system">The plugin file system helper.</param>
        void Load(QuiteExtendableDirectInterface reference, PluginFileSystem system);
        /// <summary>
        /// This is called when the plugin is shutting down.
        /// </summary>
        void Destroy();
    }
}
