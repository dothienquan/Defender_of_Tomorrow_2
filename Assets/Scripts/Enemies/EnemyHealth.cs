
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Base")]
    [SerializeField] private int startingHealth = 3;
    [SerializeField] private GameObject deathVFXPrefab;
    [SerializeField] private float knockBackThrust = 15f;
    [SerializeField] private GameObject expPickupPrefab;

    [Header("Flash Colors")]
    [Tooltip("Màu flash khi bị DoT")]
    [SerializeField] private Color dotFlashColor = new Color(1f, 0.5f, 0f); // cam
    [Tooltip("Màu flash khi bị Slow")]
    [SerializeField] private Color slowFlashColor = new Color(0.3f, 0.6f, 1f); // xanh nhạt

    private int currentHealth;
    private Knockback knockback;
    private Flash flash;
    private EnemyPathfinding pathfinding;

    // === DoT System ===
    private class DoTEffect
    {
        public int damagePerTick;
        public float tickInterval;
        public float duration;
        public float timeSinceLastTick;
        public float elapsed;

        public DoTEffect(int dmg, float interval, float dur)
        {
            damagePerTick = dmg;
            tickInterval = Mathf.Max(0.01f, interval);
            duration = Mathf.Max(0f, dur);
            timeSinceLastTick = 0f;
            elapsed = 0f;
        }
    }

    private readonly List<DoTEffect> activeDots = new List<DoTEffect>();

    private void Awake()
    {
        flash = GetComponent<Flash>();
        knockback = GetComponent<Knockback>();
        pathfinding = GetComponent<EnemyPathfinding>();
    }

    private void Start()
    {
        currentHealth = startingHealth;
    }

    private void Update()
    {
        if (activeDots.Count > 0)
        {
            for (int i = activeDots.Count - 1; i >= 0; i--)
            {
                var dot = activeDots[i];
                dot.elapsed += Time.deltaTime;
                dot.timeSinceLastTick += Time.deltaTime;

                while (dot.timeSinceLastTick >= dot.tickInterval && dot.elapsed <= dot.duration)
                {
                    dot.timeSinceLastTick -= dot.tickInterval;
                    InternalDamage(dot.damagePerTick, doKnockback: false, isDot: true);
                    if (currentHealth <= 0) break;
                }

                if (dot.elapsed >= dot.duration || currentHealth <= 0)
                {
                    activeDots.RemoveAt(i);
                }
            }
        }
    }

    public void TakeDamage(int damage)
    {
        InternalDamage(damage, doKnockback: true, isDot: false);
    }

    private void InternalDamage(int damage, bool doKnockback, bool isDot)
    {
        currentHealth -= Mathf.Max(0, damage);

        if (doKnockback)
        {
            knockback.GetKnockedBack(PlayerController.Instance.transform, knockBackThrust);
        }

        if (isDot)
            StartCoroutine(flash.FlashRoutine(dotFlashColor)); // DoT flash màu riêng
        else
            StartCoroutine(flash.FlashRoutine()); // Direct: trắng như cũ

        StartCoroutine(CheckDetectDeathRoutine());
    }

    public void ApplyDot(int damagePerTick, float tickInterval, float duration, bool allowStack)
    {
        if (!allowStack && activeDots.Count > 0)
        {
            DoTEffect strongest = activeDots[0];
            strongest.duration = Mathf.Max(strongest.duration - strongest.elapsed, duration);
            strongest.tickInterval = Mathf.Min(strongest.tickInterval, Mathf.Max(0.01f, tickInterval));
            strongest.damagePerTick = Mathf.Max(strongest.damagePerTick, damagePerTick);
        }
        else
        {
            activeDots.Add(new DoTEffect(damagePerTick, tickInterval, duration));
        }
    }

    // === Slow interface for projectiles ===
    public void ApplySlow(float multiplier, float duration, bool allowStack)
    {
        if (pathfinding == null) return;
        pathfinding.ApplySlow(multiplier, duration, allowStack);
        StartCoroutine(flash.FlashRoutine(slowFlashColor)); // flash màu slow
    }

    private IEnumerator CheckDetectDeathRoutine()
    {
        yield return new WaitForSeconds(flash.GetRestoreMatTime());
        DetectDeath();
    }

    public void DetectDeath()
    {
        if (currentHealth > 0) return;

        if (deathVFXPrefab != null)
            Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);

        GetComponent<PickUpSpawner>()?.DropItems();
        GetComponent<SpawnerSlime>()?.SpawnerSlimes();

        if (expPickupPrefab != null)
            Instantiate(expPickupPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
