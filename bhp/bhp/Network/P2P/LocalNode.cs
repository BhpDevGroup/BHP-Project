﻿using Akka.Actor;
using Bhp.IO;
using Bhp.Ledger;
using Bhp.Network.P2P.Payloads;
using Bhp.Plugins;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;

namespace Bhp.Network.P2P
{
    public class LocalNode : Peer
    {
        public class Relay { public IInventory Inventory; }
        internal class RelayDirectly { public IInventory Inventory; }
        internal class SendDirectly { public IInventory Inventory; }

        public const uint ProtocolVersion = 0;
        protected override int ConnectedMax => 10;
        protected override int UnconnectedMax => 1000;

        private readonly BhpSystem system;
        internal readonly ConcurrentDictionary<IActorRef, RemoteNode> RemoteNodes = new ConcurrentDictionary<IActorRef, RemoteNode>();

        public int ConnectedCount => RemoteNodes.Count;
        public int UnconnectedCount => UnconnectedPeers.Count;
        public static readonly uint Nonce;
        public static string UserAgent { get; set; }

        private static LocalNode singleton { get; set; }
        public static LocalNode Singleton
        {
            get
            {
                while (singleton == null) Thread.Sleep(10);
                return singleton;
            }
        }

        static LocalNode()
        {
            Random rand = new Random();
            Nonce = (uint)rand.Next();
            UserAgent = $"/{Assembly.GetExecutingAssembly().GetName().Name}:{Assembly.GetExecutingAssembly().GetVersion()}/";
        }

        public LocalNode(BhpSystem system)
        {
            lock (GetType())
            {
                if (singleton != null)
                    throw new InvalidOperationException();
                this.system = system;
                singleton = this;
            }
        }

        private void BroadcastMessage(string command, ISerializable payload = null)
        {
            BroadcastMessage(Message.Create(command, payload));
        }

        private void BroadcastMessage(Message message)
        {
            Connections.Tell(message);
        }

        private static IPEndPoint GetIPEndpointFromHostPort(string hostNameOrAddress, int port)
        {
            if (IPAddress.TryParse(hostNameOrAddress, out IPAddress ipAddress))
                return new IPEndPoint(ipAddress, port);
            IPHostEntry entry;
            try
            {
                entry = Dns.GetHostEntry(hostNameOrAddress);
            }
            catch (SocketException)
            {
                return null;
            }
            ipAddress = entry.AddressList.FirstOrDefault(p => p.AddressFamily == AddressFamily.InterNetwork || p.IsIPv6Teredo);
            if (ipAddress == null) return null;
            return new IPEndPoint(ipAddress, port);
        }

        private static IEnumerable<IPEndPoint> GetIPEndPointsFromSeedList(int seedsToTake)
        {
            if (seedsToTake > 0)
            {
                Random rand = new Random();
                foreach (string hostAndPort in Settings.Default.SeedList.OrderBy(p => rand.Next()))
                {
                    if (seedsToTake == 0) break;
                    string[] p = hostAndPort.Split(':');
                    IPEndPoint seed;
                    try
                    {
                        seed = GetIPEndpointFromHostPort(p[0], int.Parse(p[1]));
                    }
                    catch (AggregateException)
                    {
                        continue;
                    }
                    if (seed == null) continue;
                    seedsToTake--;
                    yield return seed;
                }
            }
        }

        public IEnumerable<RemoteNode> GetRemoteNodes()
        {
            return RemoteNodes.Values;
        }

        public IEnumerable<IPEndPoint> GetUnconnectedPeers()
        {
            return UnconnectedPeers;
        }

        protected override void NeedMorePeers(int count)
        {
            count = Math.Max(count, 5);
            if (ConnectedPeers.Count > 0)
            {
                BroadcastMessage("getaddr");
            }
            else
            {
                AddPeers(GetIPEndPointsFromSeedList(count));
            }
        }

        protected override void OnReceive(object message)
        {
            base.OnReceive(message);
            switch (message)
            {
                case Message msg:
                    BroadcastMessage(msg);
                    Log(msg.Command, LogLevel.Info);
                    break;
                case Relay relay:
                    OnRelay(relay.Inventory);
                    Log(relay.Inventory.InventoryType.ToString() + " OnRelay", LogLevel.Info);
                    break;
                case RelayDirectly relay:
                    OnRelayDirectly(relay.Inventory);
                    Log(relay.Inventory.InventoryType.ToString() + " OnRelayDirectly", LogLevel.Info);
                    break;
                case SendDirectly send:
                    OnSendDirectly(send.Inventory);
                    Log(send.Inventory.InventoryType.ToString() + " OnSendDirectly", LogLevel.Info);
                    break;
                case RelayResultReason reason:
                    Log(reason.ToString(), LogLevel.Info);
                    break;
            }  
        }

        private void OnRelay(IInventory inventory)
        {
            if (inventory is Transaction transaction)
                system.Consensus?.Tell(transaction);
            system.Blockchain.Tell(inventory);
        }

        private void OnRelayDirectly(IInventory inventory)
        {
            Connections.Tell(new RemoteNode.Relay { Inventory = inventory });
        }

        private void OnSendDirectly(IInventory inventory)
        {
            Connections.Tell(inventory);
        }

        public static Props Props(BhpSystem system)
        {
            return Akka.Actor.Props.Create(() => new LocalNode(system));
        }

        protected override Props ProtocolProps(object connection, IPEndPoint remote, IPEndPoint local)
        {
            return RemoteNode.Props(system, connection, remote, local);
        }

        private void Log(string message, LogLevel level = LogLevel.Info)
        {
            Plugin.Log(nameof(LocalNode), level, message);
        }
    }
}
