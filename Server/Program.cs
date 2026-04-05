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
        
        // Stats (Warrior와 동일하게 설정)
        public float MoveSpeed { get; set; } = 4.5f;
        public float SprintMultiplier { get; set; } = 1.6f;
        public float MaxStamina { get; set; } = 150f;
        public float CurrentStamina { get; set; } = 150f;
        public float StaminaRegenRate { get; set; } = 12f;
        public float SprintStaminaCost { get; set; } = 20f;
        public float GuardStaminaCost { get; set; } = 10f; // 방어 시 초당 소모 스테미나

        // Input State
        public float LastInputX { get; set; }
        public float LastInputY { get; set; }
        public bool IsSprinting { get; set; }
        public bool IsGuarding { get; set; }
        public float AimAngle { get; set; }
        public bool IsAttacking { get; set; }

        public void Update(float deltaTime)
        {
            if (deltaTime > 0.1f || deltaTime <= 0) return;

            bool isMoving = (Math.Abs(LastInputX) > 0.01f || Math.Abs(LastInputY) > 0.01f);
            float currentSpeed = MoveSpeed;

            // [기획 구현] 스테미나 0일 때 이동 속도 50% 감소 (탈진 상태)
            bool isExhausted = CurrentStamina <= 0;
            if (isExhausted)
            {
                currentSpeed *= 0.5f;
            }

            // Handle Guarding
            if (IsGuarding && !isExhausted)
            {
                // 방어 중에는 이동 속도 대폭 감소 (기획 의도에 따라 조절 가능)
                currentSpeed *= 0.3f;
                CurrentStamina -= GuardStaminaCost * deltaTime;
                if (CurrentStamina < 0) CurrentStamina = 0;
            }
            // Handle Sprinting and Stamina (Guard가 아닐 때만 Sprint 가능하도록 처리)
            else if (isMoving && IsSprinting && !isExhausted)
            {
                currentSpeed *= SprintMultiplier;
                CurrentStamina -= SprintStaminaCost * deltaTime;
                if (CurrentStamina < 0) CurrentStamina = 0;
            }
            else
            {
                // Regerate Stamina (방어 중이 아닐 때만 회복)
                if (!IsGuarding && CurrentStamina < MaxStamina)
                {
                    CurrentStamina += StaminaRegenRate * deltaTime;
                    if (CurrentStamina > MaxStamina) CurrentStamina = MaxStamina;
                }
            }

            // Move (방어 중에도 이동은 가능하지만 느림)
            X += LastInputX * currentSpeed * deltaTime;
            Y += LastInputY * currentSpeed * deltaTime;

            // Safety
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
                        player.IsSprinting = reader.GetBool();
                        player.IsGuarding = reader.GetBool();
                        player.AimAngle = reader.GetFloat();
                        player.IsAttacking = reader.GetBool();
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

                foreach (var target in players.Values)
                {
                    writer.Reset();
                    writer.Put((byte)PacketType.SPacket_PlayerState);
                    writer.Put(target.Id);
                    writer.Put(target.X);
                    writer.Put(target.Y);
                    writer.Put(target.CurrentStamina);
                    writer.Put(target.IsSprinting);
                    writer.Put(target.IsGuarding);
                    writer.Put(target.LastInputX);
                    writer.Put(target.LastInputY);
                    writer.Put(target.AimAngle);
                    writer.Put(target.IsAttacking);

                    server.SendToAll(writer, DeliveryMethod.Unreliable);
                }

                Thread.Sleep(33); // 30 FPS
            }
        }
    }
}
