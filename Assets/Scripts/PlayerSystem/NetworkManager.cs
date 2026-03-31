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
            }

            dataReader.Recycle();
        };
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
                _localPlayer.SyncState(state.Position, state.Stamina);
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

        remotePlayer?.SetState(state.Position, state.IsSprinting, state.MoveInput);
    }

    private void HandlePlayerLeave(int id)
    {
        if (_remotePlayers.TryGetValue(id, out var remotePlayer))
        {
            if (remotePlayer != null) Destroy(remotePlayer.gameObject);
            _remotePlayers.Remove(id);
        }
    }

    public void SendInput(Vector2 moveInput)
    {
        if (_server == null || _myId == -1) return;

        // Note: In a real scenario, we might want to check if the state changed
        // but for now, we'll send it as long as there's a local player
        if (_localPlayer == null) return;

        var packet = new CPacket_Input 
        { 
            MoveInput = moveInput, 
            IsSprinting = _localPlayer.IsSprinting 
        };
        
        _writer.Reset();
        packet.Serialize(_writer);
        _server.Send(_writer, DeliveryMethod.Unreliable);
    }

    private void OnDestroy()
    {
        _client?.Stop();
    }
}
