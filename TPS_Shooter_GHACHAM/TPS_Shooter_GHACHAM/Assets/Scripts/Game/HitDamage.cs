using UnityEngine;

public class HitDamage : MonoBehaviour
{
    public EnemyHealth health;          

    public void HitRaycast(FiringWeapon weapon, Vector3 direction)
    {
        health.TakeDamage(weapon.weaponDamage, direction);
    }
}
