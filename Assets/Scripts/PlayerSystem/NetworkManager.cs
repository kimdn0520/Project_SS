using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

public class NetworkManager : SingletonMonoBehaviour<NetworkManager>
{
    private EventBasedNetListener _listener;
    private NetManager _client;
    private NetPeer _server;
    private NetDataWriter _writer;

    private int _myId = -1;
    public int LocalPlayerId => _myId;
    
    private LocalPlayerController _localPlayer;
    private Dictionary<int, RemotePlayer> _remotePlayers = new Dictionary<int, RemotePlayer>();
    private Dictionary<int, DummyMonster> _monsters = new Dictionary<int, DummyMonster>();

    protected override void Awake()
    {
        base.Awake();
        _writer = new NetDataWriter();
        Application.runInBackground = true;
    }

    private void Start()
    {
        ConnectToServer();
    }

    private void Update()
    {
        _client?.PollEvents();
    }

    private void ConnectToServer()
    {
        _listener = new EventBasedNetListener();
        _client = new NetManager(_listener);
        _client.Start();
        _client.Connect("localhost", 9050, "SS_GAME_KEY");

        _listener.PeerConnectedEvent += peer =>
        {
            _server = peer;
            Debug.Log($"Connected to server!");
        };

        _listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod, channel) =>
        {
            PacketType packetType = (PacketType)dataReader.GetByte();

            switch (packetType)
            {
                case PacketType.SPacket_Welcome:
                    var welcome = new SPacket_Welcome();
                    welcome.Deserialize(dataReader);
                    _myId = welcome.MyId;
                    SpawnLocalPlayer();
                    break;

                case PacketType.SPacket_PlayerState:
                    var statePacket = new SPacket_PlayerState();
                    statePacket.Deserialize(dataReader);
                    HandlePlayerState(statePacket);
                    break;

                case PacketType.SPacket_PlayerLeave:
                    int leaveId = dataReader.GetInt();
                    HandlePlayerLeave(leaveId);
                    break;

                case PacketType.SPacket_Damage:
                    var damagePacket = new SPacket_Damage();
                    damagePacket.Deserialize(dataReader);
                    HandleDamage(damagePacket);
                    break;

                case PacketType.SPacket_MonsterState:
                    var monsterState = new SPacket_MonsterState();
                    monsterState.Deserialize(dataReader);
                    HandleMonsterState(monsterState);
                    break;

                case PacketType.SPacket_MonsterAttack:
                    var monsterAttack = new SPacket_MonsterAttack();
                    monsterAttack.Deserialize(dataReader);
                    HandleMonsterAttack(monsterAttack);
                    break;
            }

            dataReader.Recycle();
        };
    }

    private void HandleDamage(SPacket_Damage packet)
    {
        if (packet.TargetId == _myId)
        {
            if (_localPlayer != null)
            {
                _localPlayer.TakeDamage(packet.Damage, packet.Knockback);
                if (CameraShake.Instance != null) CameraShake.Instance.DamageShake();
            }
        }
        else if (_remotePlayers.TryGetValue(packet.TargetId, out var remotePlayer))
        {
            if (remotePlayer != null)
            {
                remotePlayer.TakeDamage(packet.Damage, packet.Knockback);
            }
        }
        else if (_monsters.TryGetValue(packet.TargetId, out var monster))
        {
            if (monster != null)
            {
                monster.TakeDamage(packet.Damage, packet.Knockback);
            }
        }
    }

    private void HandleMonsterState(SPacket_MonsterState packet)
    {
        if (!_monsters.TryGetValue(packet.MonsterId, out var monster))
        {
            // 1. 씬에 이미 배치된 몬스터가 있는지 먼저 찾습니다.
            DummyMonster[] existingMonsters = FindObjectsByType<DummyMonster>(FindObjectsSortMode.None);
            foreach (var m in existingMonsters)
            {
                if (m.MonsterId == packet.MonsterId)
                {
                    monster = m;
                    _monsters.Add(packet.MonsterId, monster);
                    Debug.Log($"<color=green>[Network]</color> Linked to existing Monster in scene: {packet.MonsterId}");
                    break;
                }
            }

            // 2. 없다면 새로 생성합니다.
            if (monster == null)
            {
                GameObject prefab = Resources.Load<GameObject>("Prefabs/Monster");
                if (prefab == null) prefab = Resources.Load<GameObject>("Prefabs/DummyMonster");
                
                if (prefab != null)
                {
                    GameObject go = Instantiate(prefab);
                    go.transform.SetParent(GetMonsterGroup());
                    monster = go.GetComponent<DummyMonster>();
                    monster.Init(packet.MonsterId);
                    _monsters.Add(packet.MonsterId, monster);
                    Debug.Log($"<color=green>[Network]</color> Spawned new Monster: {packet.MonsterId}");
                }
            }
        }

        if (monster != null)
        {
            monster.SetState(packet.Position, packet.MoveInput, packet.State);
        }
    }

    private void HandleMonsterAttack(SPacket_MonsterAttack packet)
    {
        if (_monsters.TryGetValue(packet.MonsterId, out var monster))
        {
            monster.PlayAttackEffect(packet.TargetPosition);
        }
    }

    private Transform GetMonsterGroup()
    {
        GameObject entities = GameObject.Find("-- ENTITIES --");
        if (entities == null) entities = new GameObject("-- ENTITIES --");

        Transform group = entities.transform.Find("Monster_Group");
        if (group == null)
        {
            GameObject groupGO = new GameObject("Monster_Group");
            groupGO.transform.SetParent(entities.transform);
            group = groupGO.transform;
        }
        return group;
    }

    public void SendHit(int targetId)
    {
        if (_server == null) return;

        var packet = new CPacket_Hit { TargetId = targetId };
        
        _writer.Reset();
        _writer.Put((byte)packet.Type);
        packet.Serialize(_writer);
        _server.Send(_writer, DeliveryMethod.ReliableOrdered);
    }

    private Transform GetPlayerGroup()
    {
        GameObject entities = GameObject.Find("-- ENTITIES --");
        if (entities == null) entities = new GameObject("-- ENTITIES --");

        Transform group = entities.transform.Find("Player_Group");
        if (group == null)
        {
            GameObject groupGO = new GameObject("Player_Group");
            groupGO.transform.SetParent(entities.transform);
            group = groupGO.transform;
        }
        return group;
    }

    private void SpawnLocalPlayer()
    {
        GameObject prefab = Resources.Load<GameObject>("Prefabs/LocalPlayer");
        if (prefab != null)
        {
            GameObject go = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            go.transform.SetParent(GetPlayerGroup()); // 규칙 적용: Player_Group 설정
            _localPlayer = go.GetComponent<LocalPlayerController>();
        }
    }

    private void HandlePlayerState(SPacket_PlayerState state)
    {
        if (float.IsNaN(state.Position.x) || float.IsInfinity(state.Position.x)) return;

        if (state.PlayerId == _myId)
        {
            if (_localPlayer != null)
            {
                _localPlayer.IsGuarding = state.IsGuarding;
                _localPlayer.SyncState(state.Position, state.Stamina, state.MaxStamina);
            }
        }
        else
        {
            UpdateRemotePlayer(state);
        }
    }

    private void UpdateRemotePlayer(SPacket_PlayerState state)
    {
        if (!_remotePlayers.TryGetValue(state.PlayerId, out var remotePlayer))
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/RemotePlayer");
            if (prefab != null)
            {
                GameObject go = Instantiate(prefab);
                go.transform.SetParent(GetPlayerGroup()); // 규칙 적용: Player_Group 설정
                remotePlayer = go.GetComponent<RemotePlayer>();
                remotePlayer.Init(state.PlayerId);
                _remotePlayers.Add(state.PlayerId, remotePlayer);
            }
        }

        if (remotePlayer != null)
        {
            remotePlayer.IsGuarding = state.IsGuarding;
            remotePlayer.SetState(state.Position, state.Stamina, state.MaxStamina, state.IsSprinting, state.MoveInput, state.AimAngle, state.IsAttacking);
        }
    }

    private void HandlePlayerLeave(int id)
    {
        if (_remotePlayers.TryGetValue(id, out var remotePlayer))
        {
            if (remotePlayer != null) Destroy(remotePlayer.gameObject);
            _remotePlayers.Remove(id);
        }
    }

    public void SendInput(Vector2 moveInput, float aimAngle, bool isAttacking)
    {
        if (_server == null || _myId == -1) return;

        // Note: In a real scenario, we might want to check if the state changed
        // but for now, we'll send it as long as there's a local player
        if (_localPlayer == null) return;

        var packet = new CPacket_Input 
        { 
            MoveInput = moveInput, 
            IsSprinting = _localPlayer.IsSprinting,
            IsGuarding = _localPlayer.IsGuarding,
            AimAngle = aimAngle,
            IsAttacking = isAttacking
        };
        
        _writer.Reset();
        _writer.Put((byte)packet.Type);
        packet.Serialize(_writer);
        _server.Send(_writer, DeliveryMethod.Unreliable);
    }

    private void OnDestroy()
    {
        _client?.Stop();
    }
}
