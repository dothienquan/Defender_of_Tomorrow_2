using UnityEngine;

public class EnemyAttackEvents : MonoBehaviour
{
    [Header("Hitboxes")]
    public Collider2D hitbox1;
    public Collider2D hitbox2;
    public Collider2D hitbox3;

    // Attack 1
    public void AE_A1_EnableHitbox() { if (hitbox1) hitbox1.enabled = true; }
    public void AE_A1_DisableHitbox() { if (hitbox1) hitbox1.enabled = false; }

    // Attack 2
    public void AE_A2_EnableHitbox() { if (hitbox2) hitbox2.enabled = true; }
    public void AE_A2_DisableHitbox() { if (hitbox2) hitbox2.enabled = false; }

    // Attack 3
    public void AE_A3_EnableHitbox() { if (hitbox3) hitbox3.enabled = true; }
    public void AE_A3_DisableHitbox() { if (hitbox3) hitbox3.enabled = false; }
}
