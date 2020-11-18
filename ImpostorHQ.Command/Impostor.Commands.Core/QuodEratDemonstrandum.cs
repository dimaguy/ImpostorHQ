using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Impostor.Api.Net;

namespace Impostor.Commands.Core
{
    public class QuodEratDemonstrandum
    {
        public class QuiteElegantDirectory
        {
            //nothing special going on here, but it is technically a quantum effect :)
            //P.S this is a joke
            public Thread ObserverThread { get; private set; }
            private bool DoObserve { get; set; }
            private List<IClientPlayer> Players { get; set; }

            public QuiteElegantDirectory()
            {
                this.Players = new List<IClientPlayer>();
                DoObserve = true;
                ObserverThread = new Thread(ObserverCallback);
                ObserverThread.Start();
            }

            //they are going to be entangled with the players in the enumerable list
            public async Task EntanglePlayer(IClientPlayer player)
            {
                if (player == null) return;
                await Task.Run(() =>
                {
                    lock (Players)
                    {
                        Players.Add(player);
                    }
                }).ConfigureAwait(false);
            }

            public void RemoveDeadPlayer(IClientPlayer player)
            {
                lock (Players)
                {
                    if (player!=null && Players.Contains(player)) Players.Remove(player);
                }
            }

            private void ObserverCallback()
            {
                while (DoObserve)
                {
                    Observe().ConfigureAwait(false);
                    Thread.Sleep(1000);
                }
            }

            public async Task Observe()
            {
                lock(Players)
                    if (Players.Count == 0)
                        return;
                await Task.Run(()  =>
                {
                    lock (Players)
                    {
                        foreach (var clientPlayer in Players)
                        {
                            if(CollapseSuperposition(clientPlayer))
                            {
                                RemoveDeadPlayer(clientPlayer);
                            }
                        }
                    }
                }).ConfigureAwait(false);
            }

            public List<IClientPlayer> AcquireList()
            {
                lock (Players)
                {
                    if (Players.Count == 0) return new List<IClientPlayer>();
                    return Players.ToList();
                }
            }

            private bool CollapseSuperposition(IClientPlayer clientPlayer)
            {
                //we observe the state, so we collapse the superposition.
                return (clientPlayer == null || clientPlayer.Client.Connection == null ||
                        !clientPlayer.Client.Connection.IsConnected);
            }
            
            public void Shutdown()
            {
                DoObserve = false;
            }
        }
    }
}
