﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BiomeMap.Shared.Net;
using BiomeMap.Shared.Net.Serialization;
using Fleck;
using Google.Protobuf;
using log4net;

namespace BiomeMap.Plugin.Net
{
    public static class WsServer
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WsServer));


        private static readonly object _sync = new object();
        public static List<WsConnection> Connections = new List<WsConnection>();


        private static readonly WebSocketServer _server = new WebSocketServer("ws://0.0.0.0:5001");


        public static void OnOpen(WsConnection c)
        {
            lock (_sync)
            {
                Connections.Add(c);
                c.SendPacket(new MapConfigPacket()
                {
                    Config = BiomeMapPlugin.Config
                });

                foreach (var levelRunner in BiomeMapPlugin.Instance.LevelRunners)
                {
                    c.SendPacket(new LevelMetaPacket()
                    {
                        LevelId = levelRunner.Map.Meta.Id,
                        Meta = levelRunner.Map.Meta
                    });
                }
            }
        }

        public static void OnClose(WsConnection c)
        {
            lock (_sync)
            {
                Connections.Remove(c);
            }
        }

        public static void SendPacket(IWebSocketConnection connection, IPacket packet)
        {
            lock (_sync)
            {
                var msg = packet.Encode();
                connection.Send(msg);
            }
        }

        public static void BroadcastTileUpdate(TileUpdatePacket update)
        {
            Parallel.ForEach(Connections.ToArray(), c => c.SendTileUpdate(update));
        }

        public static void BroadcastPacket(IPacket packet)
        {
            lock (_sync)
            {
                Parallel.ForEach(Connections, c =>
                {
                    c.SendPacket(packet);
                });
            }
        }

        public static void Start()
        {

            _server.Start(c =>
            {
                var wsConn = new WsConnection(c);

                c.OnOpen += () => OnOpen(wsConn);
                c.OnClose += () => OnClose(wsConn);
            });
            Log.InfoFormat("Web server started.");
        }


        public static void Stop()
        {
            _server.Dispose();
            Log.InfoFormat("Web server stopped.");
        }

    }
}
