using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using LiteNetLib;
using LiteNetLib.Utils;

namespace Server
{
    public enum PacketType : byte
    {
        SPacket_Welcome = 0,
        CPacket_Input = 1,
        SPacket_PlayerState = 2,
        SPacket_PlayerJoin = 3,
        SPacket_PlayerLeave = 4
    }

    class Player
    {
        public int Id { get; set; }
        public NetPeer Peer { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float MoveSpeed { get; set; } = 5.0f;
        public float LastInputX { get; set; }
        public float LastInputY { get; set; }

        public void Update(float deltaTime)
        {
            // deltaTime이 너무 크거나(예: 일시정지) 음수면 무시
            if (deltaTime > 0.1f || deltaTime < 0) return;

            X += LastInputX * MoveSpeed * deltaTime;
            Y += LastInputY * MoveSpeed * deltaTime;

            // 좌표가 튀는 것 방지 (안전 장치)
            if (float.IsNaN(X) || float.IsInfinity(X)) X = 0;
            if (float.IsNaN(Y) || float.IsInfinity(Y)) Y = 0;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            EventBasedNetListener listener = new EventBasedNetListener();
            NetManager server = new NetManager(listener);
            server.Start(9050);

            Console.WriteLine("[Server] Project_SS Server Started on port 9050...");

            Dictionary<int, Player> players = new Dictionary<int, Player>();
            NetDataWriter writer = new NetDataWriter();

            listener.ConnectionRequestEvent += request => request.AcceptIfKey("SS_GAME_KEY");

            listener.PeerConnectedEvent += peer =>
            {
                Console.WriteLine($"[Server] Player Joined (ID: {peer.Id})");
                players[peer.Id] = new Player { Id = peer.Id, Peer = peer };

                writer.Reset();
                writer.Put((byte)PacketType.SPacket_Welcome);
                writer.Put(peer.Id);
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
            };

            listener.PeerDisconnectedEvent += (peer, info) =>
            {
                Console.WriteLine($"[Server] Player Left (ID: {peer.Id})");
                players.Remove(peer.Id);
                
                writer.Reset();
                writer.Put((byte)PacketType.SPacket_PlayerLeave);
                writer.Put(peer.Id);
                server.SendToAll(writer, DeliveryMethod.ReliableOrdered);
            };

            listener.NetworkReceiveEvent += (fromPeer, reader, method, channel) =>
            {
                if (players.TryGetValue(fromPeer.Id, out var player))
                {
                    byte type = reader.GetByte();
                    if (type == (byte)PacketType.CPacket_Input)
                    {
                        player.LastInputX = reader.GetFloat();
                        player.LastInputY = reader.GetFloat();
                    }
                }
                reader.Recycle();
            };

            DateTime lastTime = DateTime.UtcNow;
            while (true)
            {
                server.PollEvents();

                DateTime now = DateTime.UtcNow;
                float deltaTime = (float)(now - lastTime).TotalSeconds;
                lastTime = now;

                foreach (var player in players.Values)
                {
                    player.Update(deltaTime);
                }

                // 모든 플레이어에게 상태 전송 (이동 데이터 전용)
                foreach (var target in players.Values)
                {
                    writer.Reset();
                    writer.Put((byte)PacketType.SPacket_PlayerState);
                    writer.Put(target.Id);
                    writer.Put(target.X);
                    writer.Put(target.Y);
                    writer.Put(100f);

                    server.SendToAll(writer, DeliveryMethod.Unreliable);
                }

                Thread.Sleep(33); // 30 FPS
            }
        }
    }
}
