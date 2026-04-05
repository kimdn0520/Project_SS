using UnityEngine;
using LiteNetLib.Utils;

public enum PacketType : byte
{
    SPacket_Welcome = 0,
    CPacket_Input = 1,
    SPacket_PlayerState = 2,
    SPacket_PlayerJoin = 3,
    SPacket_PlayerLeave = 4
}

public interface IPacket
{
    PacketType Type { get; }
    void Serialize(NetDataWriter writer);
    void Deserialize(NetDataReader reader);
}

public struct SPacket_Welcome : IPacket
{
    public PacketType Type => PacketType.SPacket_Welcome;
    public int MyId;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)Type);
        writer.Put(MyId);
    }

    public void Deserialize(NetDataReader reader)
    {
        MyId = reader.GetInt();
    }
}

public struct CPacket_Input : IPacket
{
    public PacketType Type => PacketType.CPacket_Input;
    public Vector2 MoveInput;
    public bool IsSprinting;
    public bool IsGuarding;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)Type);
        writer.Put(MoveInput.x);
        writer.Put(MoveInput.y);
        writer.Put(IsSprinting);
        writer.Put(IsGuarding);
    }

    public void Deserialize(NetDataReader reader)
    {
        MoveInput.x = reader.GetFloat();
        MoveInput.y = reader.GetFloat();
        IsSprinting = reader.GetBool();
        IsGuarding = reader.GetBool();
    }
}

public struct SPacket_PlayerState : IPacket
{
    public PacketType Type => PacketType.SPacket_PlayerState;
    public int PlayerId;
    public Vector2 Position;
    public float Stamina;
    public bool IsSprinting;
    public bool IsGuarding;
    public Vector2 MoveInput; // For animation sync on remote clients

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)Type);
        writer.Put(PlayerId);
        writer.Put(Position.x);
        writer.Put(Position.y);
        writer.Put(Stamina);
        writer.Put(IsSprinting);
        writer.Put(IsGuarding);
        writer.Put(MoveInput.x);
        writer.Put(MoveInput.y);
    }

    public void Deserialize(NetDataReader reader)
    {
        PlayerId = reader.GetInt();
        Position.x = reader.GetFloat();
        Position.y = reader.GetFloat();
        Stamina = reader.GetFloat();
        IsSprinting = reader.GetBool();
        IsGuarding = reader.GetBool();
        MoveInput.x = reader.GetFloat();
        MoveInput.y = reader.GetFloat();
    }
}
