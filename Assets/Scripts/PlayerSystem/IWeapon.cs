using UnityEngine;

public interface IWeapon
{
    float AngleOffset { get; }
    Transform Pivot { get; }
    
    void EnableHit(Vector2 direction);
    void DisableHit();
}
