using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Impostor.Commands.Core;
using Impostor.Commands.Core.QuantumExtensionDirector;

namespace ImpostorHQ.Plugin.HighCourt
{
    public class MainClass : IPlugin
    {
        public string Name => "High Court";

        public string Author => "anti";

        public uint HqVersion => 4;
        public QuiteExtendableDirectInterface PluginBase { get; private set; }
        public PluginFileSystem FileSystem { get; private set; }
        public CourtConfig Configuration { get; private set; }
        public JusticeSystem HighCourt { get; private set; }

        #region Commands
        public const string PlayerReportCommand = "/report";
        public const string DashboardBansMessage = "/bans";
        public const string DashboardBanIpAddress = "/banip";
        public const string DashboardBanIpAddressBlind = "/banipblind";
        public const string DashboardUnBanAddress = "/unbanip";
        public const string DashboardReloadBans = "/reloadbans";

        #endregion

        public void Destroy()
        {
            
        }

        public void Load(QuiteExtendableDirectInterface reference, PluginFileSystem system)
        {
            this.PluginBase = reference;
            this.FileSystem = system;
            if (system.IsDefault())
            {
                system.Save(CourtConfig.GetDefault());
                this.Configuration = CourtConfig.GetDefault();
            }
            else
            {
                this.Configuration = system.ReadConfig<CourtConfig>();
            }

            this.HighCourt = new JusticeSystem(Path.Combine(system.Store,"bans"),Configuration.ReportsPerBan,reference.Logger,reference.ChatInterface,reference.UnsafeDirectReference);
            this.HighCourt.OnPlayerBanned += PlayerBanned;

            PluginBase.ApiServer.RegisterCommand(DashboardUnBanAddress, " <IP address> => this is the equivalent of deleting a ban file and reloading the bans. The said player will be unbanned from the server, if he is banned.");
            PluginBase.ApiServer.RegisterCommand(DashboardBanIpAddress, " <ip address> => will permanently ban the IP address. The players must be connected.");
            PluginBase.ApiServer.RegisterCommand(DashboardBanIpAddressBlind, " <ip address> => just like the above, but the player does not need to be connected. Warning: he will not be kicked if he is connected to a game. Use the above command if that is your intention.");
            PluginBase.ApiServer.RegisterCommand(DashboardReloadBans, "=> this will reload the bans from the disk. This can be useful if you need to remove a ban, and don't want to restart the server. To do that, just delete the ban from the disk and execute this command.");
            PluginBase.ApiServer.RegisterCommand(DashboardBansMessage, "=> will list the current permanent bans.");
            if (Configuration.EnablePlayerReports) PluginBase.EventListener.RegisterCommand(PlayerReportCommand);


            reference.OnDashboardCommandReceived += DashboardCommandReceived;
            reference.ChatInterface.OnCommandInvoked += PlayerCommandReceived;
            reference.EventListener.OnPlayerSpawnedFirst+= (evt) =>
            {
                HighCourt.HandleSpawn(evt);
            };

        }

        private void PlayerCommandReceived(string command, string data, Impostor.Api.Events.Player.IPlayerChatEvent source)
        {
            switch (command)
            {
                case PlayerReportCommand:
                {
                    if (string.IsNullOrEmpty(data))
                    {
                        PluginBase.ChatInterface.SafeMultiMessage(source.Game, "Invalid command data. Please use /help for more information.", Structures.BroadcastType.Error, destination: source.ClientPlayer);
                        return;
                    }
                    HighCourt.HandleReport(data, source);
                    break;
                }
            }
        }

        private void DashboardCommandReceived(string command, string data, bool single, Fleck.IWebSocketConnection source)
        {
            switch (command)
            {
                case DashboardBansMessage:
                {
                    if (!single)
                    {
                        PluginBase.ApiServer.PushTo("Invalid command data.", "sysinfo", Structures.MessageFlag.ConsoleLogMessage, source);
                        break;
                    }

                    lock (HighCourt.PermanentBans)
                    {
                        string response = $"  Total bans : {HighCourt.PermanentBans.Count}\n";
                        if (HighCourt.PermanentBans.Count > 0)
                        {
                            //there are bans, so we add them to our message.
                            foreach (var ban in HighCourt.PermanentBans)
                            {
                                response += "  IPA : " + ban.Target.ToString() + $" / {ban.TargetName}\n";
                            }
                        }

                        PluginBase.ApiServer.PushTo(response, "sysinfo", Structures.MessageFlag.ConsoleLogMessage, source);
                    }

                    break;
                }
                case DashboardBanIpAddress:
                {
                    if (single)
                    {
                        PluginBase.ApiServer.PushTo("Invalid command data.", "sysinfo", Structures.MessageFlag.ConsoleLogMessage, source);
                        break;
                    }

                    if (IPAddress.TryParse(data, out IPAddress address))
                    {
                        if (HighCourt.BanPlayer(address, source.ConnectionInfo.ClientIpAddress))
                        {
                            PluginBase.ApiServer.Push(
                                $"The target [{address}] has been banned permanently by {source.ConnectionInfo.ClientIpAddress}!",
                                "(SERVER/CRITICAL/WIDE)", Structures.MessageFlag.ConsoleLogMessage);
                        }
                        else
                        {
                            PluginBase.ApiServer.PushTo("Could not find player.", Structures.ServerSources.DebugSystem,
                                Structures.MessageFlag.ConsoleLogMessage, source);
                        }
                    }
                    else
                    {
                        PluginBase.ApiServer.PushTo("Invalid command data.", "sysinfo", Structures.MessageFlag.ConsoleLogMessage, source);
                    }

                    break;
                }
                case DashboardBanIpAddressBlind:
                {
                    if (single)
                    {
                        PluginBase.ApiServer.PushTo("Invalid command data.", "sysinfo", Structures.MessageFlag.ConsoleLogMessage, source);
                        break;
                    }

                    if (IPAddress.TryParse(data, out IPAddress address))
                    {
                        var report = new JusticeSystem.Report
                        {
                            Messages = new List<string>
                            {
                                "<adminsys / " + DateTime.Now + ">"
                            },
                            Sources = new List<string>
                            {
                                source.ConnectionInfo.ClientIpAddress
                            },
                            Target = address.ToString(),
                            TargetName = "<unknown>",
                            MinutesRemaining = 0,
                            TotalReports = 0
                        };

                        HighCourt.AddPermBan(report);
                        PluginBase.ApiServer.Push(
                            $"The target [{address}] has been blindly banned by {source.ConnectionInfo.ClientIpAddress}!",
                            "(SERVER/CRITICAL/WIDE)", Structures.MessageFlag.ConsoleLogMessage);
                    }
                    else
                    {
                        PluginBase.ApiServer.PushTo("Invalid command data.", "sysinfo", Structures.MessageFlag.ConsoleLogMessage, source);
                    }
                    break;
                }
                case DashboardUnBanAddress:
                {
                    if (single)
                    {
                        PluginBase.ApiServer.PushTo("Invalid command data.", "sysinfo", Structures.MessageFlag.ConsoleLogMessage, source);
                        break;
                    }
                    if (HighCourt.RemoveBan(data))
                    {
                        PluginBase.ApiServer.PushTo($"The target player has been unbanned.",
                            Structures.ServerSources.CommandSystem, Structures.MessageFlag.ConsoleLogMessage,
                            source);
                        PluginBase.ApiServer.Push(
                            $"A player with the IP address [{data}] has been unbanned by {source.ConnectionInfo.ClientIpAddress}.",
                            Structures.ServerSources.DebugSystemCritical, Structures.MessageFlag.ConsoleLogMessage);

                    }
                    else
                    {
                        PluginBase.ApiServer.PushTo($"Error: the target player [{data}] is not banned.",
                            Structures.ServerSources.DebugSystem, Structures.MessageFlag.ConsoleLogMessage, source);
                    }

                    break;
                }
                case DashboardReloadBans:
                {
                    if (!single)
                    {
                        PluginBase.ApiServer.PushTo("Invalid command data.", "sysinfo", Structures.MessageFlag.ConsoleLogMessage, source);
                        break;
                    }
                    HighCourt.ReloadBans();
                    break;

                }
            }
        }
        /// <summary>
        /// This is called when a player is banned by reports.
        /// </summary>
        /// <param name="rep"></param>
        private void PlayerBanned(JusticeSystem.Report rep)
        {
            PluginBase.ApiServer.Push($"Player {rep.TargetName} / {rep.Target} was banned permanently.", "reportsys", Structures.MessageFlag.ConsoleLogMessage, null);
        }
    }

    public class CourtConfig
    {
        public ushort ReportsPerBan { get; set; }
        public bool EnablePlayerReports { get; set; }
        public static CourtConfig GetDefault()
        {
            return new CourtConfig(){ReportsPerBan = 10,EnablePlayerReports = true};
        }
    }
}
