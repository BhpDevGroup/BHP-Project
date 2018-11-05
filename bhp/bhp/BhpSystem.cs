﻿using Akka.Actor;
using Bhp.Consensus;
using Bhp.Ledger;
using Bhp.Network.P2P;
using Bhp.Network.RPC;
using Bhp.Persistence;
using Bhp.Plugins;
using Bhp.Wallets;
using System;
using System.Net;

namespace Bhp
{
    public class BhpSystem : IDisposable
    {
        public ActorSystem ActorSystem = ActorSystem.Create(nameof(BhpSystem),
            $"akka {{ log-dead-letters = off }}" +
            $"blockchain-mailbox {{ mailbox-type: \"{typeof(BlockchainMailbox).AssemblyQualifiedName}\" }}" +
            $"task-manager-mailbox {{ mailbox-type: \"{typeof(TaskManagerMailbox).AssemblyQualifiedName}\" }}" +
            $"remote-node-mailbox {{ mailbox-type: \"{typeof(RemoteNodeMailbox).AssemblyQualifiedName}\" }}" +
            $"protocol-handler-mailbox {{ mailbox-type: \"{typeof(ProtocolHandlerMailbox).AssemblyQualifiedName}\" }}" +
            $"consensus-service-mailbox {{ mailbox-type: \"{typeof(ConsensusServiceMailbox).AssemblyQualifiedName}\" }}");
        public IActorRef Blockchain;
        public IActorRef LocalNode;
        internal IActorRef TaskManager;
        public IActorRef Consensus;
        private RpcServer rpcServer;

        public BhpSystem(Store store)
        {
            this.Blockchain = ActorSystem.ActorOf(Ledger.Blockchain.Props(this, store));
            this.LocalNode = ActorSystem.ActorOf(Network.P2P.LocalNode.Props(this));
            this.TaskManager = ActorSystem.ActorOf(Network.P2P.TaskManager.Props(this));
            Plugin.LoadPlugins(this);
        }

        public void Dispose()
        {
            rpcServer?.Dispose();
            ActorSystem.Stop(LocalNode);
            ActorSystem.Dispose();
        }

        public void StartConsensus(Wallet wallet)
        {
            Consensus = ActorSystem.ActorOf(ConsensusService.Props(this, wallet));
            Consensus.Tell(new ConsensusService.Start());
        }

        public void StartNode(int port = 0, int ws_port = 0)
        {
            LocalNode.Tell(new Peer.Start
            {
                Port = port,
                WsPort = ws_port
            });
        }

        public void StartRpc(IPAddress bindAddress, int port, Wallet wallet = null,string walletPassword=null,bool isAutoLock=false, string sslCert = null, string password = null,
           string[] trustedAuthorities = null, Fixed8 maxGasInvoke = default(Fixed8))
        {
            rpcServer = new RpcServer(this, wallet, walletPassword,isAutoLock, maxGasInvoke);
            rpcServer.Start(bindAddress, port, sslCert, password, trustedAuthorities);
        }

        public void OpenWallet(Wallet wallet, string password, bool isAutoLock)
        {
            if (rpcServer == null)
            {
                rpcServer = new RpcServer(this, wallet, password, isAutoLock, Fixed8.Zero);
            }
            rpcServer.SetWallet(wallet, password, isAutoLock);
        }
    }
}
