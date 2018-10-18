﻿using Microsoft.AspNetCore.Http;
using Bhp.IO.Data.LevelDB;
using Bhp.IO.Json;
using Bhp.Network.RPC;

namespace Bhp.Plugins
{
    public class LogReader : Plugin, IRpcPlugin
    {
        private readonly DB db = DB.Open(Settings.Default.Path, new Options { CreateIfMissing = true });

        public override string Name => "ApplicationLogs";

        public LogReader()
        {
            System.ActorSystem.ActorOf(Logger.Props(System.Blockchain, db));
        }

        public JObject OnProcess(HttpContext context, string method, JArray _params)
        {
            if (method != "getapplicationlog") return null;
            UInt256 hash = UInt256.Parse(_params[0].AsString());
            if (!db.TryGet(ReadOptions.Default, hash.ToArray(), out Slice value))
                throw new RpcException(-100, "Unknown transaction");
            return JObject.Parse(value.ToString());
        }
    }
}
