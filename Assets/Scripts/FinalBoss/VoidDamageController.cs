using UnityEngine;

public class VoidDamageController : MonoBehaviour
{
    [Header("Void Area (world units)")]
    public float voidRadius = 7f; // bán kính gameplay

    [Header("Damage")]
    [Range(0f, 1f)]
    public float damagePercent = 0.2f; // 20%
    public float tickInterval = 1f;

    private float timer;
    private Transform player;
    private Collider2D playerCollider;

    private void Start()
    {
        if (PlayerHealth.Instance != null)
        {
            player = PlayerHealth.Instance.transform;
            playerCollider = PlayerHealth.Instance.GetComponent<Collider2D>();
        }
        else
        {
            Debug.LogError("VoidDamageController: Không tìm thấy PlayerHealth.Instance.");
        }
    }

    private void Update()
    {
        if (player == null) return;

        timer += Time.deltaTime;
        if (timer < tickInterval) return;
        timer = 0f;

        // 1) Player có trong vùng void không?
        float distToCenter = Vector2.Distance(player.position, transform.position);
        if (distToCenter > voidRadius)
            return;

        // 2) Player có trong BẤT KỲ safe zone nào không?
        if (PlayerInsideSafeZone())
            return;

        // 3) Không an toàn -> ăn damage
        int dmg = Mathf.RoundToInt(PlayerHealth.Instance.MaxHealth * damagePercent);
        PlayerHealth.Instance.TakeDamage(dmg, transform);
    }

    private bool PlayerInsideSafeZone()
    {
        var zones = SafeZone.ActiveZones;
        if (zones == null || zones.Count == 0)
            return false;

        Vector2 playerPos = playerCollider != null
            ? (Vector2)playerCollider.bounds.center
            : (Vector2)player.position;

        foreach (var z in zones)
        {
            if (z == null || !z.IsActive) continue;

            float r = z.CurrentRadius;
            float distSqr = (playerPos - (Vector2)z.transform.position).sqrMagnitude;

            if (distSqr <= r * r)
                return true;
        }

        return false;
    }

    /* ───────────────────── GIZMOS ───────────────────── */

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.6f, 0f, 1f, 0.18f);
        Gizmos.DrawSphere(transform.position, voidRadius);

        Gizmos.color = new Color(1f, 0f, 1f, 0.9f);
        DrawCircle(transform.position, voidRadius, 64);
    }

    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prev = center + new Vector3(radius, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float rad = angleStep * i * Mathf.Deg2Rad;
            Vector3 cur = center + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * radius;
            Gizmos.DrawLine(prev, cur);
            prev = cur;
        }
    }
}
