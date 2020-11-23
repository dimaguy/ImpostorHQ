using System;

namespace Impostor.Commands.Core.QuantumExtensionDirector
{
    public interface IPlugin
    {
        string Name { get; }
        string Author { get; }
        UInt32 HqVersion { get; }

        void Load(QuiteExtendableDirectInterface reference, PluginFileSystem system);
        void Destroy();
    }
}
