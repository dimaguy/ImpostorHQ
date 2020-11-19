using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Impostor.Commands.Core;

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
