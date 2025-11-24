using UnityEngine;
using System.Collections.Generic;

public class VoidDamageController : MonoBehaviour
{
    public List<SafeZone> safeZones = new();
    public float damagePercent = 0.2f; // 20%
    public float tickInterval = 1f;

    private float timer;
    private Transform player;

    private void Start()
    {
        player = PlayerHealth.Instance.transform;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer < tickInterval) return;
        timer = 0f;

        if (PlayerInsideSafeZone())
            return;

        int dmg = Mathf.RoundToInt(PlayerHealth.Instance.MaxHealth * damagePercent);
        PlayerHealth.Instance.TakeDamage(dmg, transform);
    }

    private bool PlayerInsideSafeZone()
    {
        foreach (var z in safeZones)
        {
            if (z == null || !z.IsActive) continue;

            float dist = Vector2.Distance(player.position, z.transform.position);
            if (dist <= z.CurrentRadius)
                return true;
        }
        return false;
    }
}

