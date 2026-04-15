using UnityEngine;
using LiteNetLib.Utils;

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

public struct SPacket_MonsterState : IPacket
{
    public PacketType Type => PacketType.SPacket_MonsterState;
    public int MonsterId;
    public Vector2 Position;
    public Vector2 MoveInput;
    public MonsterState State;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(MonsterId);
        writer.Put(Position.x);
        writer.Put(Position.y);
        writer.Put(MoveInput.x);
        writer.Put(MoveInput.y);
        writer.Put((byte)State);
    }

    public void Deserialize(NetDataReader reader)
    {
        MonsterId = reader.GetInt();
        Position.x = reader.GetFloat();
        Position.y = reader.GetFloat();
        MoveInput.x = reader.GetFloat();
        MoveInput.y = reader.GetFloat();
        State = (MonsterState)reader.GetByte();
    }
}

public struct SPacket_MonsterAttack : IPacket
{
    public PacketType Type => PacketType.SPacket_MonsterAttack;
    public int MonsterId;
    public Vector2 TargetPosition;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(MonsterId);
        writer.Put(TargetPosition.x);
        writer.Put(TargetPosition.y);
    }

    public void Deserialize(NetDataReader reader)
    {
        MonsterId = reader.GetInt();
        TargetPosition.x = reader.GetFloat();
        TargetPosition.y = reader.GetFloat();
    }
}

public interface IPacket
{
    PacketType Type { get; }
    void Serialize(NetDataWriter writer);
    void Deserialize(NetDataReader reader);
}

public struct CPacket_Hit : IPacket
{
    public PacketType Type => PacketType.CPacket_Hit;
    public int TargetId;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(TargetId);
    }

    public void Deserialize(NetDataReader reader)
    {
        TargetId = reader.GetInt();
    }
}

public struct SPacket_Damage : IPacket
{
    public PacketType Type => PacketType.SPacket_Damage;
    public int TargetId;
    public float Damage;
    public Vector2 Knockback;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(TargetId);
        writer.Put(Damage);
        writer.Put(Knockback.x);
        writer.Put(Knockback.y);
    }

    public void Deserialize(NetDataReader reader)
    {
        TargetId = reader.GetInt();
        Damage = reader.GetFloat();
        Knockback.x = reader.GetFloat();
        Knockback.y = reader.GetFloat();
    }
}

public struct SPacket_Welcome : IPacket
{
    public PacketType Type => PacketType.SPacket_Welcome;
    public int MyId;

    public void Serialize(NetDataWriter writer)
    {
        // Type은 NetworkManager나 Server에서 개별적으로 Put하므로 여기선 데이터만 넣습니다.
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
    public float AimAngle;
    public bool IsAttacking;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(MoveInput.x);
        writer.Put(MoveInput.y);
        writer.Put(IsSprinting);
        writer.Put(IsGuarding);
        writer.Put(AimAngle);
        writer.Put(IsAttacking);
    }

    public void Deserialize(NetDataReader reader)
    {
        MoveInput.x = reader.GetFloat();
        MoveInput.y = reader.GetFloat();
        IsSprinting = reader.GetBool();
        IsGuarding = reader.GetBool();
        AimAngle = reader.GetFloat();
        IsAttacking = reader.GetBool();
    }
}

public struct SPacket_PlayerState : IPacket
{
    public PacketType Type => PacketType.SPacket_PlayerState;
    public int PlayerId;
    public Vector2 Position;
    public float Stamina;
    public float MaxStamina;
    public bool IsSprinting;
    public bool IsGuarding;
    public Vector2 MoveInput; // For animation sync on remote clients
    public float AimAngle;
    public bool IsAttacking;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(PlayerId);
        writer.Put(Position.x);
        writer.Put(Position.y);
        writer.Put(Stamina);
        writer.Put(MaxStamina);
        writer.Put(IsSprinting);
        writer.Put(IsGuarding);
        writer.Put(MoveInput.x);
        writer.Put(MoveInput.y);
        writer.Put(AimAngle);
        writer.Put(IsAttacking);
    }

    public void Deserialize(NetDataReader reader)
    {
        PlayerId = reader.GetInt();
        Position.x = reader.GetFloat();
        Position.y = reader.GetFloat();
        Stamina = reader.GetFloat();
        MaxStamina = reader.GetFloat();
        IsSprinting = reader.GetBool();
        IsGuarding = reader.GetBool();
        MoveInput.x = reader.GetFloat();
        MoveInput.y = reader.GetFloat();
        AimAngle = reader.GetFloat();
        IsAttacking = reader.GetBool();
    }
}
