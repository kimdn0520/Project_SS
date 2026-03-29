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
    
    private Dictionary<int, RemotePlayer> _remotePlayers = new Dictionary<int, RemotePlayer>();

    protected override void Awake()
    {
        base.Awake();
        _writer = new NetDataWriter();

        // 창 포커스가 나가도 게임이 멈추지 않도록 설정 (멀티플레이 테스트 필수)
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
            Debug.Log($"Connected to server! Handshaking...");
        };

        _listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod, channel) =>
        {
            // [중요] GetByte()로 헤더를 소비해야 뒤의 데이터 정렬이 맞습니다.
            PacketType packetType = (PacketType)dataReader.GetByte();

            switch (packetType)
            {
                case PacketType.SPacket_Welcome:
                    var welcome = new SPacket_Welcome();
                    welcome.Deserialize(dataReader);
                    _myId = welcome.MyId;
                    Debug.Log($"Welcome! My Server ID: {_myId}");
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

    private void SpawnLocalPlayer()
    {
        GameObject prefab = Resources.Load<GameObject>("Prefabs/LocalPlayer");
        if (prefab != null)
        {
            if (FindObjectOfType<LocalPlayerController>() == null)
            {
                Instantiate(prefab, Vector3.zero, Quaternion.identity);
            }
        }
    }

    private void HandlePlayerState(SPacket_PlayerState state)
    {
        // 안전성 체크: 좌표가 유효하지 않으면 무시
        if (float.IsNaN(state.Position.x) || float.IsInfinity(state.Position.x)) return;

        if (state.PlayerId == _myId)
        {
            // 내 위치 보정 (필요 시)
        }
        else
        {
            UpdateRemotePlayerPosition(state.PlayerId, state.Position);
        }
    }

    private void HandlePlayerLeave(int id)
    {
        if (_remotePlayers.TryGetValue(id, out var remotePlayer))
        {
            if (remotePlayer != null) Destroy(remotePlayer.gameObject);
            _remotePlayers.Remove(id);
            Debug.Log($"Player Left: {id}");
        }
    }

    public void SendInput(Vector2 moveInput)
    {
        if (_server == null || _myId == -1) return;

        var packet = new CPacket_Input { MoveInput = moveInput };
        _writer.Reset();
        packet.Serialize(_writer);

        _server.Send(_writer, DeliveryMethod.Unreliable);
    }

    private void UpdateRemotePlayerPosition(int id, Vector2 position)
    {
        if (!_remotePlayers.TryGetValue(id, out var remotePlayer))
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/RemotePlayer");
            if (prefab != null)
            {
                GameObject go = Instantiate(prefab);
                remotePlayer = go.GetComponent<RemotePlayer>();
                remotePlayer.Init(id);
                _remotePlayers.Add(id, remotePlayer);
                Debug.Log($"New Remote Player Joined: {id}");
            }
        }

        remotePlayer?.SetTargetPosition(position);
    }

    private void OnDestroy()
    {
        _client?.Stop();
    }
}
