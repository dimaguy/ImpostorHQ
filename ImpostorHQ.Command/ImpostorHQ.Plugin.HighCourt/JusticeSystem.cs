using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using Impostor.Api.Events.Player;
using Impostor.Commands.Core;
using Microsoft.Extensions.Logging;

namespace ImpostorHQ.Plugin.HighCourt
{
    public class JusticeSystem
    {
        #region Members
        //  we need a storage for reports (that are not bans, yet)
        public List<JusticeSystem.Report> IpReports = new List<JusticeSystem.Report>();
        //  we need a storage for permanent bans. This is also present on the DISK.
        public List<JusticeSystem.Report> PermanentBans = new List<JusticeSystem.Report>();
        //  the global logger.
        private ILogger Logger { get; set; }
        //  this indicates how many reports are required to automatically issue a ban.
        public readonly ushort ReportsPerBan;
        //  this stores the path to our database.
        //  it is a folder storing files for each permanent ban.
        private string BanFolder { get; set; }
        //  a chat interface, used to interact with witnesses.

        private GameCommandChatInterface ChatInterface { get; set; }
        private Class Manager { get; set; }
        #endregion

        /// <summary>
        /// This will initialize a new instance of the JusticeSystem class. It is an example extension, which handles banning cheaters.
        /// </summary>
        /// <param name="banFolder">The path to where the database should be stored.</param>
        /// <param name="reportsPerBan">How many reports are required in order to issue an automatic ban.</param>
        /// <param name="logger">The global logger.</param>
        /// <param name="chatInterface">A chat interface, in order to interact with the players.</param>
        public JusticeSystem(string banFolder, ushort reportsPerBan, ILogger logger, GameCommandChatInterface chatInterface,Class Master)
        {
            this.ReportsPerBan = reportsPerBan;
            this.BanFolder = banFolder;
            this.Logger = logger;
            this.ChatInterface = chatInterface;
            this.Manager = Master;
            //we can now read our database (if we have one).
            LoadPermanentBans();
        }
        /// <summary>
        /// This will read and load permanent bans off the disk, if we have any.
        /// </summary>
        private void LoadPermanentBans()
        {
            if (!Directory.Exists(BanFolder))
            {
                Directory.CreateDirectory(BanFolder);
                return;
            }
            foreach (var file in Directory.GetFiles(BanFolder))
            {
                if (file.Contains("ban-"))
                {
                    var report = JsonSerializer.Deserialize<JusticeSystem.Report>(File.ReadAllText(file));
                    lock (PermanentBans) PermanentBans.Add(report);
                    OnBanRead?.Invoke(report);
                }
            }

            lock (PermanentBans) Logger.LogInformation($"ImpostorHQ : Loaded {PermanentBans.Count} bans.");
        }
        /// <summary>
        /// Call this whenever the report command is fired. This will handle reporting, logging, and banning.
        /// </summary>
        /// <param name="data">The command data.</param>
        /// <param name="source">The source of the report.</param>
        public void HandleReport(string data, IPlayerChatEvent source)
        {
            if (data.Count(x => x == "'"[0]) != 2)
            {
                ChatInterface.SafeMultiMessage(source.Game,
                    "Invalid format. Please use : \"/report hacking Player's name 'Describe the cheat here'\"!",
                    Structures.BroadcastType.Error, "(server/private)", source.ClientPlayer);
                return;
            }

            if (data.StartsWith("hacking "))
            {
                data = data.Remove(0, 8);
                int pFrom = data.IndexOf("'", StringComparison.InvariantCultureIgnoreCase) + 1;
                int pTo = data.LastIndexOf("'", StringComparison.CurrentCultureIgnoreCase);
                var message = data.Substring(pFrom, pTo - pFrom);
                data = new string(data.Take(pFrom - 2 /*we need to remove the ' and the space.*/).ToArray());
                foreach (var client in source.Game.Players)
                {
                    if (client.Character.PlayerInfo.PlayerName.Equals(data))
                    {
                        lock (IpReports)
                        {
                            bool updated = false;
                            for (int i = 0; i < IpReports.Count; i++)
                            {
                                if (IpReports[i].Target.Equals(client.Client.Connection.EndPoint.Address.ToString()))
                                {
                                    updated = true;
                                    if (IpReports[i].TotalReports >= ReportsPerBan)
                                    {
                                        client.BanAsync();
                                        AddPermBan(IpReports[i]);
                                        IpReports.Remove(IpReports[i]);
                                        ChatInterface.SafeMultiMessage(source.Game,
                                            $"\"{client.Character.PlayerInfo.PlayerName}\" has been permanently banned.",
                                            Structures.BroadcastType.Warning);
                                    }
                                    else
                                    {
                                        lock (IpReports)
                                        {
                                            updated = true;
                                            if (!IpReports[i].Sources.Contains(source.ClientPlayer.Client
                                                .Connection.EndPoint.Address.ToString()))
                                            {
                                                IpReports[i].TotalReports += 1;
                                                IpReports[i].Messages.Add(message);
                                                IpReports[i].Sources.Add(source.ClientPlayer.Client
                                                    .Connection
                                                    .EndPoint.Address.ToString());
                                                ChatInterface.SafeMultiMessage(source.Game,
                                                    $"Your report has been filed successfully. The offender has {IpReports[i].TotalReports} complaints now.",
                                                    Structures.BroadcastType.Information, "(server complaints/private)",
                                                    source.ClientPlayer);

                                            }
                                            else
                                            {
                                                ChatInterface.SafeMultiMessage(source.Game,
                                                    $"You cannot report the offender again. He will be taken care of.",
                                                    Structures.BroadcastType.Error, "(server/error/private)",
                                                    source.ClientPlayer);
                                            }
                                        }

                                    }
                                }
                            }

                            if (!updated)
                            {
                                var report = new JusticeSystem.Report
                                {
                                    TargetName = client.Character.PlayerInfo.PlayerName,
                                    TotalReports = 1,
                                    Target = client.Client.Connection.EndPoint.Address.ToString(),
                                    Sources = new List<string>()
                                };
                                report.Sources.Add(source.ClientPlayer.Client.Connection.EndPoint.Address.ToString());
                                lock (IpReports) IpReports.Add(report);
                                ChatInterface.SafeMultiMessage(source.Game,
                                    $"A criminal record has been created for {client.Character.PlayerInfo.PlayerName}!",
                                    Structures.BroadcastType.Information, "(server complaints/private)",
                                    source.ClientPlayer);
                            }
                        }

                        return;
                    }
                }

                ChatInterface.SafeMultiMessage(source.Game, "Could not find player", Structures.BroadcastType.Warning,
                    "(server/warn)", source.ClientPlayer);
            }
        }
        /// <summary>
        /// This will add a permanent ban to both the memory list and the disk database.
        /// </summary>
        /// <param name="rep"></param>
        public void AddPermBan(JusticeSystem.Report rep)
        {
            lock (PermanentBans)
            {
                PermanentBans.Add(rep);
                OnPlayerBanned?.Invoke(rep);
            }

            File.WriteAllText(Path.Combine(BanFolder, $"ban-{rep.Target}.json"), JsonSerializer.Serialize(rep));
        }
        public void ReloadBans()
        {
            lock(PermanentBans) PermanentBans.Clear();
            LoadPermanentBans();
        }
        /// <summary>
        /// This will remove a ban, if it exists.
        /// </summary>
        /// <param name="address">The address of the offender.</param>
        /// <returns>True if the ban was found and deleted.</returns>
        public bool RemoveBan(string address)
        {
            lock (PermanentBans)
            {
                foreach (var report in PermanentBans)
                {
                    if (report.Target.Equals(address))
                    {
                        PermanentBans.Remove(report);
                        File.Delete(Path.Combine(BanFolder, "ban-" + address + ".json"));
                        OnPlayerPardoned?.Invoke(report);
                        return true;
                    }
                }
            }

            return false;
        }
        /// <summary>
        /// Call this whenever a player spawns. It will handle any permanent bans.
        /// </summary>
        /// <param name="evt"></param>
        public void HandleSpawn(IPlayerSpawnedEvent evt)
        {
            lock (PermanentBans)
            {
                foreach (var ban in PermanentBans)
                {
                    if (ban.Target.Equals(evt.ClientPlayer.Client.Connection.EndPoint.Address.ToString()))
                    {
                        evt.ClientPlayer.BanAsync();
                    }
                }
            }
        }
        /// <summary>
        /// This will permanently ban a player. The player must be connected to the server.
        /// </summary>
        /// <param name="addr">The player's address.</param>
        /// <param name="dashboard">The address of the dashboard from where the command originated.</param>
        /// <returns>True if the player was found and banned.</returns>
        public bool BanPlayer(IPAddress addr,string dashboard)
        {
            bool found = false;
            foreach (var possibleTarget in Manager.GetPlayers())
            {
                if(possibleTarget == null || possibleTarget.Client.Connection==null ||possibleTarget.Character == null) continue;
                if (possibleTarget.Client.Connection.EndPoint.Address.Equals(addr))
                {
                    //we found our target!
                    possibleTarget.BanAsync();
                    if (!found)
                    {
                        found = true;
                        var report = new JusticeSystem.Report
                        {
                            Messages = new List<string>
                            {
                                "<adminsys / " + DateTime.Now.ToString() + ">"
                            },
                            Sources = new List<string>
                            {
                                dashboard
                            },
                            Target = possibleTarget.Client.Connection.EndPoint.Address.ToString(),
                            TargetName = possibleTarget.Character.PlayerInfo.PlayerName,

                            MinutesRemaining = 0,
                            TotalReports = 0
                        };
                        AddPermBan(report);
                    }
                }
            }

            return found;
        }

        #region Events
        public delegate void PlayerBanned(JusticeSystem.Report report);

        public event PlayerBanned OnPlayerBanned;

        public delegate void PlayerPardoned(JusticeSystem.Report report);

        public event PlayerPardoned OnPlayerPardoned;

        public delegate void BanLoaded(JusticeSystem.Report rep);

        public event BanLoaded OnBanRead;
        #endregion

        [Serializable]
        public class Report
        {
            /// <summary>
            /// The supposed offender's IPA.
            /// </summary>
            public string Target { get; set; }
            /// <summary>
            /// The name of the offender.
            /// </summary>
            public string TargetName { get; set; }
            /// <summary>
            /// The witnesses's IPAs.
            /// </summary>
            public List<string> Sources { get; set; }
            /// <summary>
            /// The testimonials.
            /// </summary>
            public List<string> Messages { get; set; }
            /// <summary>
            /// How many complaints there are against the offender.
            /// </summary>
            public ushort TotalReports { get; set; }
            /// <summary>
            /// How much jail time the offender gets.
            /// </summary>
            public uint MinutesRemaining { get; set; }
        }
    }
}