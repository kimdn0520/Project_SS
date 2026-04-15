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
        SPacket_PlayerLeave = 4,
        CPacket_Hit = 5,
        SPacket_Damage = 6,
        SPacket_MonsterState = 7,
        SPacket_MonsterAttack = 8
    }

    public enum MonsterState : byte
    {
        Idle = 0,
        Chase = 1,
        Attack = 2
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
        public float MaxHealth { get; set; } = 100f;
        public float CurrentHealth { get; set; } = 100f;

        public float LastInputX { get; set; }
        public float LastInputY { get; set; }
        public bool IsSprinting { get; set; }
        public bool IsGuarding { get; set; }
        public float AimAngle { get; set; }
        public bool IsAttacking { get; set; }

        public void TakeDamage(float damage)
        {
            CurrentHealth -= damage;
            if (CurrentHealth < 0) CurrentHealth = 0;
            Console.WriteLine($"[Combat] Player {Id} took {damage} damage. HP: {CurrentHealth}");
        }

        public void Update(float deltaTime)
        {
            float targetSpeed = IsSprinting ? MoveSpeed * 1.6f : (IsGuarding ? MoveSpeed * 0.4f : MoveSpeed);
            
            // 단순 선형 이동 (서버 가감속 생략하여 반응성 우선)
            VelX = LastInputX * targetSpeed;
            VelY = LastInputY * targetSpeed;

            X += VelX * deltaTime;
            Y += VelY * deltaTime;
        }
    }

    class Monster
    {
        public int Id { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float MoveSpeed { get; set; } = 3.5f;
        public float MaxHealth { get; set; } = 100f;
        public float CurrentHealth { get; set; } = 100f;
        
        public MonsterState State { get; private set; } = MonsterState.Idle;
        public float MoveInputX { get; private set; }
        public float MoveInputY { get; private set; }

        private float detectionRange = 6f; // 10f에서 6f로 하향
        private float attackRange = 1.8f;
        private float attackCooldown = 2.5f;
        private float attackTimer = 0f;
        
        private float idleTimer = 0f;
        private Random random = new Random();

        public Action<Monster, Player> OnAttack;

        public void TakeDamage(float damage)
        {
            CurrentHealth -= damage;
            Console.WriteLine($"[Monster] Monster {Id} HP: {CurrentHealth}");
        }

        public void Update(float deltaTime, Dictionary<int, Player> players)
        {
            if (attackTimer > 0) attackTimer -= deltaTime;

            Player target = null;
            float minDist = float.MaxValue;

            foreach (var p in players.Values)
            {
                if (p.Peer == null) continue;
                float dist = (float)Math.Sqrt(Math.Pow(X - p.X, 2) + Math.Pow(Y - p.Y, 2));
                if (dist < detectionRange && dist < minDist)
                {
                    minDist = dist;
                    target = p;
                }
            }

            if (target != null)
            {
                if (minDist <= attackRange)
                {
                    State = MonsterState.Attack;
                    MoveInputX = 0; MoveInputY = 0;
                    if (attackTimer <= 0)
                    {
                        attackTimer = attackCooldown;
                        OnAttack?.Invoke(this, target);
                    }
                }
                else
                {
                    State = MonsterState.Chase;
                    float dirX = target.X - X;
                    float dirY = target.Y - Y;
                    float len = (float)Math.Sqrt(dirX * dirX + dirY * dirY);
                    MoveInputX = dirX / len;
                    MoveInputY = dirY / len;
                }
            }
            else
            {
                idleTimer -= deltaTime;
                if (idleTimer <= 0)
                {
                    idleTimer = (float)random.NextDouble() * 2f + 1f;
                    MoveInputX = (float)random.NextDouble() * 2 - 1;
                    MoveInputY = (float)random.NextDouble() * 2 - 1;
                }
                State = MonsterState.Idle;
            }

            float speed = State == MonsterState.Chase ? MoveSpeed : MoveSpeed * 0.4f;
            if (State == MonsterState.Attack) speed = 0;

            X += MoveInputX * speed * deltaTime;
            Y += MoveInputY * speed * deltaTime;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            EventBasedNetListener listener = new EventBasedNetListener();
            NetManager server = new NetManager(listener);
            server.Start(9050);
            Console.WriteLine("[Server] AI System Online.");

            Dictionary<int, Player> players = new Dictionary<int, Player>();
            Dictionary<int, Monster> monsters = new Dictionary<int, Monster>();

            // 더미 몬스터 생성
            Monster dummy = new Monster { Id = 999, X = 10f, Y = 0f };
            dummy.OnAttack = (m, target) => {
                NetDataWriter w = new NetDataWriter();
                w.Put((byte)PacketType.SPacket_MonsterAttack);
                w.Put(m.Id); w.Put(target.X); w.Put(target.Y);
                server.SendToAll(w, DeliveryMethod.ReliableOrdered);
                
                target.TakeDamage(10f);
                BroadcastDamage(server, target.Id, 10f, 0, 0);
            };
            monsters[dummy.Id] = dummy;
            
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
                    else if (type == (byte)PacketType.CPacket_Hit) {
                        int targetId = reader.GetInt();
                        if (monsters.TryGetValue(targetId, out var m)) {
                            m.TakeDamage(25f);
                            BroadcastDamage(server, m.Id, 25f, m.X - p.X, m.Y - p.Y);
                        }
                    }
                }
                reader.Recycle();
            };

            DateTime lastTime = DateTime.UtcNow;
            while (true) {
                server.PollEvents();
                float deltaTime = (float)(DateTime.UtcNow - lastTime).TotalSeconds;
                lastTime = DateTime.UtcNow;

                foreach (var p in players.Values) p.Update(deltaTime);
                foreach (var m in monsters.Values) m.Update(deltaTime, players);

                foreach (var t in players.Values) {
                    if (t.Peer == null) continue;
                    writer.Reset(); writer.Put((byte)PacketType.SPacket_PlayerState);
                    writer.Put(t.Id); writer.Put(t.X); writer.Put(t.Y);
                    writer.Put(100f); writer.Put(100f); // Stamina dummy
                    writer.Put(t.IsSprinting); writer.Put(t.IsGuarding);
                    writer.Put(t.LastInputX); writer.Put(t.LastInputY);
                    writer.Put(t.AimAngle); writer.Put(t.IsAttacking);
                    server.SendToAll(writer, DeliveryMethod.Unreliable);
                }

                foreach (var m in monsters.Values) {
                    writer.Reset(); writer.Put((byte)PacketType.SPacket_MonsterState);
                    writer.Put(m.Id); writer.Put(m.X); writer.Put(m.Y);
                    writer.Put(m.MoveInputX); writer.Put(m.MoveInputY);
                    writer.Put((byte)m.State);
                    server.SendToAll(writer, DeliveryMethod.Unreliable);
                }

                Thread.Sleep(20); // 50FPS 동기화
            }
        }

        static void BroadcastDamage(NetManager server, int targetId, float damage, float dx, float dy) {
            NetDataWriter w = new NetDataWriter();
            w.Put((byte)PacketType.SPacket_Damage);
            w.Put(targetId); w.Put(damage);
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (len > 0) { dx /= len; dy /= len; }
            w.Put(dx * 5f); w.Put(dy * 5f);
            server.SendToAll(w, DeliveryMethod.ReliableOrdered);
        }
    }
}
