using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Impostor.Api.Innersloth.Customization;
using Impostor.Commands.Core;
using Impostor.Commands.Core.QuantumExtensionDirector;
using Impostor.Commands.Core.SELF;

namespace ImpostorHQ.Plugin.Fashionable
{
    public class MainClass : IPlugin
    {
        public string Name => "Fashion Plugin";

        public string Author => "anti";

        public uint HqVersion => 1;

        public QuiteExtendableDirectInterface PluginBase { get; private set; }
        public ItemConverter SkinProvider { get; private set; }
        public Dictionary<string,Skin> SkinList { get; set; }
        public string SkinStr { get; set; }
        public void Destroy()
        {
        }

        public void Load(QuiteExtendableDirectInterface reference)
        {
            SkinList = new Dictionary<string, Skin>();
            if (!Directory.Exists(Constants.SkinPath)) Directory.CreateDirectory(Constants.SkinPath);
            this.PluginBase = reference;
            this.SkinProvider = new ItemConverter();
            foreach (var preSetSkin in Skin.FromDir(Constants.SkinPath))
            {
                SkinList.Add(preSetSkin.Name,preSetSkin);
                SkinStr += $"\"{preSetSkin.Name}\" ";
            }

            if (!String.IsNullOrEmpty(SkinStr)) SkinStr = $" / {SkinStr} (sets the predefined styles)>";
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
            try
            {
                switch (command)
                {
                    case Constants.PlayerCommands.FashionCommand:
                    {
                        if (string.IsNullOrEmpty(data))
                        {
                            PluginBase.ChatInterface.SafeMultiMessage(source.Game, $"Usage: /fashion new (random style) {SkinStr}", Structures.BroadcastType.Error, destination: source.ClientPlayer);
                            return;
                        }
                        else if (data.Equals("new"))
                        {
                            var randomSkin = Skin.GetRandomSkin(SkinProvider);
                            await source.ClientPlayer.Character.SetSkinAsync(randomSkin.Clothes).ConfigureAwait(false);
                            await source.ClientPlayer.Character.SetHatAsync(randomSkin.Hat).ConfigureAwait(false);
                            await source.ClientPlayer.Character.SetPetAsync(randomSkin.Pet).ConfigureAwait(false);
                        }

                        else if (SkinList.ContainsKey(data))
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
}
