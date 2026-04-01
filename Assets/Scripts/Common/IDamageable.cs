using UnityEngine;

public interface IDamageable
{
    /// <summary>
    /// 데미지를 입히는 메서드
    /// </summary>
    /// <param name="damage">입힐 데미지 수치</param>
    /// <param name="knockback">넉백 벡터 (방향 * 힘)</param>
    void TakeDamage(float damage, Vector2 knockback);
}
