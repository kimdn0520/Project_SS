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
        public float VelX { get; set; }
        public float VelY { get; set; }
        
        public float MoveSpeed { get; set; } = 5f;
        public float SprintMultiplier { get; set; } = 1.6f;
        public float MaxStamina { get; set; } = 150f;
        public float CurrentStamina { get; set; } = 150f;
        public float StaminaRegenRate { get; set; } = 15f;
        public float SprintStaminaCost { get; set; } = 20f;
        public float GuardStaminaCost { get; set; } = 10f;
        public float AttackStaminaCost { get; set; } = 15f;

        private float staminaRegenTimer = 0f;
        private const float REGEN_DELAY = 1.0f; // 행동 후 회복 지연 시간
        private bool isExhausted = false; // 스태미너 0 이하일 때의 탈진 상태

        // 클라이언트와 동일한 가감속 상수
        private const float ACCELERATION = 120f;
        private const float DECELERATION = 100f;

        public float LastInputX { get; set; }
        public float LastInputY { get; set; }
        public bool IsSprinting { get; set; }
        public bool IsGuarding { get; set; }
        public float AimAngle { get; set; }
        public bool IsAttacking { get; set; }
        private bool prevIsAttacking = false;

        public void Update(float deltaTime)
        {
            if (deltaTime > 0.1f || deltaTime <= 0) return;

            float targetSpeed = MoveSpeed;
            
            // 1. 공격 스태미너 소모 (Rising Edge 감지)
            if (IsAttacking && !prevIsAttacking)
            {
                if (CurrentStamina >= AttackStaminaCost)
                {
                    CurrentStamina -= AttackStaminaCost;
                    staminaRegenTimer = REGEN_DELAY;
                }
                else
                {
                    // 스태미너 부족 시 공격 불가 상태는 클라이언트에서 이미 체크하지만, 
                    // 서버에서도 정합성을 위해 필요하다면 추가 로직 구현 가능.
                }
            }
            prevIsAttacking = IsAttacking;

            // 2. 달리기/가드 스태미너 소모 및 속도 조절
            bool canSprint = IsSprinting && CurrentStamina > 0 && !isExhausted && (Math.Abs(LastInputX) > 0.01f || Math.Abs(LastInputY) > 0.01f);
            bool canGuard = IsGuarding && CurrentStamina > 0 && !isExhausted;

            if (canGuard)
            {
                targetSpeed *= 0.4f;
                CurrentStamina -= GuardStaminaCost * deltaTime;
                staminaRegenTimer = REGEN_DELAY;
            }
            else if (canSprint)
            {
                targetSpeed *= SprintMultiplier;
                CurrentStamina -= SprintStaminaCost * deltaTime;
                staminaRegenTimer = REGEN_DELAY;
            }

            // 3. 탈진 및 회복 로직
            if (CurrentStamina <= 0)
            {
                CurrentStamina = 0;
                isExhausted = true;
            }
            
            if (isExhausted)
            {
                targetSpeed *= 0.5f;
                // 탈진 상태에서는 스태미너가 일정 이상(예: 30%) 차야 해제
                if (CurrentStamina >= MaxStamina * 0.3f) 
                {
                    isExhausted = false;
                }
            }

            // 회복 지연 및 자동 회복
            if (!canSprint && !canGuard && !IsAttacking)
            {
                if (staminaRegenTimer > 0)
                {
                    staminaRegenTimer -= deltaTime;
                }
                else if (CurrentStamina < MaxStamina)
                {
                    float regenMultiplier = isExhausted ? 0.6f : 1.0f; // 탈진 시 회복 속도 페널티
                    CurrentStamina += StaminaRegenRate * regenMultiplier * deltaTime;
                    if (CurrentStamina > MaxStamina) CurrentStamina = MaxStamina;
                }
            }

            // [서버 가감속 구현] 클라이언트의 MoveTowards와 동일한 로직
            float targetVelX = LastInputX * targetSpeed;
            float targetVelY = LastInputY * targetSpeed;
            float accelRate = (Math.Abs(LastInputX) > 0.01f || Math.Abs(LastInputY) > 0.01f) ? ACCELERATION : DECELERATION;

            VelX = MoveTowards(VelX, targetVelX, accelRate * deltaTime);
            VelY = MoveTowards(VelY, targetVelY, accelRate * deltaTime);

            X += VelX * deltaTime;
            Y += VelY * deltaTime;
        }

        private float MoveTowards(float current, float target, float maxDelta)
        {
            if (Math.Abs(target - current) <= maxDelta) return target;
            return current + Math.Sign(target - current) * maxDelta;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            EventBasedNetListener listener = new EventBasedNetListener();
            NetManager server = new NetManager(listener);
            server.Start(9050);
            Console.WriteLine("[Server] Snappy Movement Server Started...");

            Dictionary<int, Player> players = new Dictionary<int, Player>();
            NetDataWriter writer = new NetDataWriter();

            listener.ConnectionRequestEvent += request => request.AcceptIfKey("SS_GAME_KEY");
            listener.PeerConnectedEvent += peer => {
                players[peer.Id] = new Player { Id = peer.Id, Peer = peer };
                NetDataWriter w = new NetDataWriter();
                w.Put((byte)PacketType.SPacket_Welcome); w.Put(peer.Id);
                peer.Send(w, DeliveryMethod.ReliableOrdered);
            };
            listener.PeerDisconnectedEvent += (peer, info) => players.Remove(peer.Id);
            listener.NetworkReceiveEvent += (fromPeer, reader, method, channel) => {
                if (players.TryGetValue(fromPeer.Id, out var p)) {
                    byte type = reader.GetByte();
                    if (type == (byte)PacketType.CPacket_Input) {
                        p.LastInputX = reader.GetFloat(); p.LastInputY = reader.GetFloat();
                        p.IsSprinting = reader.GetBool(); p.IsGuarding = reader.GetBool();
                        p.AimAngle = reader.GetFloat(); p.IsAttacking = reader.GetBool();
                    }
                }
                reader.Recycle();
            };

            DateTime lastTime = DateTime.UtcNow;
            while (true) {
                server.PollEvents();
                DateTime now = DateTime.UtcNow;
                float deltaTime = (float)(now - lastTime).TotalSeconds;
                lastTime = now;
                foreach (var p in players.Values) p.Update(deltaTime);
                foreach (var t in players.Values) {
                    writer.Reset(); writer.Put((byte)PacketType.SPacket_PlayerState);
                    writer.Put(t.Id); writer.Put(t.X); writer.Put(t.Y);
                    writer.Put(t.CurrentStamina); writer.Put(t.MaxStamina); writer.Put(t.IsSprinting); writer.Put(t.IsGuarding);
                    writer.Put(t.LastInputX); writer.Put(t.LastInputY);
                    writer.Put(t.AimAngle); writer.Put(t.IsAttacking);
                    server.SendToAll(writer, DeliveryMethod.Unreliable);
                }
                Thread.Sleep(33);
            }
        }
    }
}
