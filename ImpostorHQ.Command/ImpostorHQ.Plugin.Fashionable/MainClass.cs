using System;
using System.IO;
using Impostor.Api.Innersloth;
using Impostor.Commands.Core;
using System.Collections.Generic;
using Impostor.Commands.Core.SELF;
using Microsoft.Extensions.Logging;
using Impostor.Commands.Core.QuantumExtensionDirector;

namespace ImpostorHQ.Plugin.Fashionable
{
    public class MainClass : IPlugin
    {
        public string Name => "Fashion Plugin";

        public string Author => "anti";

        public uint HqVersion => 4;

        public string SkinDir { get; private set; }

        public QuiteExtendableDirectInterface PluginBase { get; private set; }
        public ItemConverter SkinProvider { get; private set; }
        public Dictionary<string,Skin> SkinList { get; set; }
        public FashionConfig Config { get; set; }
        public string Help { get; private set; }
        public void Destroy()
        {
        }

        public void Load(QuiteExtendableDirectInterface reference,PluginFileSystem pfs)
        {
            SkinDir = Path.Combine(pfs.Store, Constants.SkinPath);
            SkinList = new Dictionary<string, Skin>();
            if (!Directory.Exists(SkinDir)) Directory.CreateDirectory(SkinDir);
            this.PluginBase = reference;

            if (pfs.IsDefault())
            {
                //the first time the plugin is run. We will create a config.
                var config = new FashionConfig(){ AllowPreSetSkins = true, AllowRandomSkins = true, AllowInGame = false };
                pfs.Save(config);
                this.Config = config;
            }
            else
            {
                //the plugin has been run before, we can load the config.
                this.Config = pfs.ReadConfig<FashionConfig>();
                if (!Config.AllowPreSetSkins && !Config.AllowRandomSkins)
                {
                    reference.Logger.LogError("Fashion : Critical error - invalid config. Reverting back to the default config.");
                    Config = new FashionConfig() { AllowPreSetSkins = true, AllowRandomSkins = true,AllowInGame = false};
                    pfs.Save(Config);
                }
            }

            this.SkinProvider = new ItemConverter();
            string skins = " ";
            if (Config.AllowPreSetSkins)
            {
                foreach (var preSetSkin in Skin.FromDir(SkinDir))
                {
                    SkinList.Add(preSetSkin.Name, preSetSkin);
                    skins += "'" + preSetSkin.Name + "', ";
                }

                if (SkinList.Count > 0)
                {
                    skins = skins.Remove(skins.Length - 3, 2);
                }
                reference.Logger.LogInformation($"Fashion: loaded {SkinList.Count} skins.");
            }

            Help += "Usage: \n";
            if (Config.AllowRandomSkins)
            {
                Help += "/fashion new => gets a random style.";
            }

            if (Config.AllowPreSetSkins)
            {
                Help += $"\n/fashion {skins} => sets the pre-defined style.";
            }
            RegisterCommands();
        }

        private void RegisterCommands()
        {
            PluginBase.ChatInterface.RegisterCommand("/fashion");
            PluginBase.ChatInterface.OnCommandInvoked += ChatInterface_OnCommandInvoked;
        }

        private  async void ChatInterface_OnCommandInvoked(string command, string data, Impostor.Api.Events.Player.IPlayerChatEvent source)
        {
            
            if (source == null || source.ClientPlayer.Character == null) return;
            if(source.ClientPlayer.Game.GameState == GameStates.Started)
                if (!Config.AllowInGame)
                    return;
            try
            {
                switch (command)
                {
                    case Constants.PlayerCommands.FashionCommand:
                    {
                        if (string.IsNullOrEmpty(data))
                        {
                            PluginBase.ChatInterface.SafeMultiMessage(source.Game, Help, Structures.BroadcastType.Error, destination: source.ClientPlayer);
                            return;
                        }
                        else if (data.Equals("new") && Config.AllowRandomSkins)
                        {
                            var randomSkin = Skin.GetRandomSkin(SkinProvider);
                            await source.ClientPlayer.Character.SetSkinAsync(randomSkin.Clothes).ConfigureAwait(false);
                            await source.ClientPlayer.Character.SetHatAsync(randomSkin.Hat).ConfigureAwait(false);
                            await source.ClientPlayer.Character.SetPetAsync(randomSkin.Pet).ConfigureAwait(false);
                        }

                        else if (SkinList.ContainsKey(data) && Config.AllowPreSetSkins)
                        {
                            var skin = SkinList[data];
                            await source.ClientPlayer.Character.SetSkinAsync(skin.Clothes).ConfigureAwait(false);
                            await source.ClientPlayer.Character.SetHatAsync(skin.Hat).ConfigureAwait(false);
                            await source.ClientPlayer.Character.SetPetAsync(skin.Pet).ConfigureAwait(false);
                        }
                        else
                        {
                            PluginBase.ChatInterface.SafeMultiMessage(source.Game, "Invalid skin.", Structures.BroadcastType.Error, destination: source.ClientPlayer);
                        }
                        break;
                    }
                }

            }
            catch (Exception e)
            {
                PluginBase.LogManager.LogError("Fashion: " + e.Message,Shared.ErrorLocation.Plugin);
            }
            
        }
    }

    [Serializable]
    public class FashionConfig
    {
        public bool AllowRandomSkins { get; set; }
        public bool AllowPreSetSkins { get; set; }
        public bool AllowInGame { get; set; }
    }
}
