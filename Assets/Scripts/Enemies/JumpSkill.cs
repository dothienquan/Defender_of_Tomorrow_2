using System.Collections;
using UnityEngine;
using System.Reflection;

/// <summary>
/// Kỹ năng "Jump" (nhảy parabol tới vị trí Player khi AttackZone kích hoạt).
/// - Gắn script này vào enemy và set trong EnemyAI.enemyType (IEnemy).
/// - Không chỉnh EnemyHealth: tự đọc %HP nếu có (HealthFraction), nếu không có sẽ
///   thử đọc currentHP/maxHP bằng reflection an toàn.
/// - HP càng thấp -> tốc độ nhảy càng cao.
/// - Gây sát thương khi chạm đất (AOE nhỏ) tại điểm đáp.
/// </summary>
public class Jump : MonoBehaviour, IEnemy
{
    [Header("Jump Motion")]
    [SerializeField] private float baseJumpSpeed = 8f;              // tốc độ cơ bản dùng để tính thời gian bay
    [SerializeField] private float maxSpeedMultiplierAtLowHP = 2f;  // hệ số ở 0% HP (từ 1 -> hệ số này)
    [SerializeField] private float arcHeight = 2.5f;                // độ cao đỉnh parabol
    [SerializeField] private float minJumpDuration = 0.15f;         // để tránh nhảy quá "giật"
    [SerializeField] private float maxJumpDuration = 1.5f;          // giới hạn trên

    [Header("Damage On Land")]
    [SerializeField] private int damage = 10;
    [SerializeField] private float damageRadius = 0.6f;
    [SerializeField] private LayerMask damageLayers;
    [SerializeField] private bool flipSpriteTowardTarget = true;

    [Header("Optional FX")]
    [SerializeField] private string jumpTriggerName = "Jump";
    [SerializeField] private GameObject landVFX;

    private Transform target;            // Player
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Component enemyHealth;       // Không phụ thuộc kiểu cụ thể
    private Coroutine jumpRoutine;
    private int jumpHash;

    private void Awake()
    {
        target = PlayerController.Instance != null ? PlayerController.Instance.transform : null;
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Thử lấy EnemyHealth nếu có (không yêu cầu có)
        enemyHealth = GetComponent(typeof(MonoBehaviour).Assembly.GetType("EnemyHealth")) ?? GetComponent("EnemyHealth");

        jumpHash = !string.IsNullOrEmpty(jumpTriggerName) ? Animator.StringToHash(jumpTriggerName) : 0;
    }

    public void Attack()
    {
        if (target == null) return;

        // Flip hướng nếu có SpriteRenderer
        if (flipSpriteTowardTarget && spriteRenderer != null)
        {
            spriteRenderer.flipX = (transform.position.x - target.position.x) > 0f;
        }

        if (animator != null && jumpHash != 0)
        {
            animator.SetTrigger(jumpHash);
        }

        // Lấy ảnh mục tiêu tại thời điểm bắt đầu (nhảy tới vị trí này, không bám theo realtime)
        Vector3 targetPos = target.position;
        targetPos.z = transform.position.z;

        if (jumpRoutine != null) StopCoroutine(jumpRoutine);
        jumpRoutine = StartCoroutine(JumpParabolaTo(targetPos));
    }

    private IEnumerator JumpParabolaTo(Vector3 targetPos)
    {
        Vector3 start = transform.position;
        float distance = Vector2.Distance(start, targetPos);

        // Tính thời lượng nhảy dựa vào "tốc độ hiện tại"
        float speed = CurrentSpeed();
        float duration = Mathf.Clamp(distance / Mathf.Max(0.01f, speed), minJumpDuration, maxJumpDuration);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float clampedT = Mathf.Clamp01(t);

            // Nội suy đường thẳng rồi cộng thêm offset parabol: y += 4h t(1-t)
            Vector3 pos = Vector3.Lerp(start, targetPos, clampedT);
            float parabola = 4f * arcHeight * clampedT * (1f - clampedT);
            pos.y += parabola;

            transform.position = pos;
            yield return null;
        }

        // Đáp đất -> gây sát thương AOE nhỏ
        DoLandEffects();
        jumpRoutine = null;
    }

    private void DoLandEffects()
    {
        if (landVFX != null)
        {
            Instantiate(landVFX, transform.position, Quaternion.identity);
        }

        if (damage > 0 && damageRadius > 0f)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, damageRadius, damageLayers);
            foreach (var h in hits)
            {
                // Ưu tiên gọi TakeDamage(int) hoặc Damage(int) nếu có
                h.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
                h.SendMessage("Damage", damage, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    private float CurrentSpeed()
    {
        float frac = GetHealthFractionSafe(); // 0..1
        if (frac >= 0.5f) return baseJumpSpeed;

        // HP < 50% -> scale tuyến tính từ 1 -> maxSpeedMultiplierAtLowHP khi HP tiến về 0%
        float t = (0.5f - frac) / 0.5f; // 0..1 (50% -> 0%)
        float mult = Mathf.Lerp(1f, maxSpeedMultiplierAtLowHP, Mathf.Clamp01(t));
        return baseJumpSpeed * mult;
    }

    /// <summary>
    /// Trả về tỉ lệ máu (0..1) một cách an toàn, không yêu cầu chỉnh EnemyHealth.
    /// Thứ tự ưu tiên:
    /// 1) Property float HealthFraction
    /// 2) Field int currentHP, maxHP
    /// Không có thì trả về 1.
    /// </summary>
    private float GetHealthFractionSafe()
    {
        if (enemyHealth == null) return 1f;

        var type = enemyHealth.GetType();

        // Property HealthFraction
        var prop = type.GetProperty("HealthFraction", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (prop != null && prop.PropertyType == typeof(float))
        {
            try
            {
                object value = prop.GetValue(enemyHealth, null);
                return Mathf.Clamp01((float)value);
            }
            catch { }
        }

        // Field currentHP / maxHP
        var curField = type.GetField("currentHP", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var maxField = type.GetField("maxHP", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (curField != null && maxField != null && curField.FieldType == typeof(int) && maxField.FieldType == typeof(int))
        {
            try
            {
                int cur = (int)curField.GetValue(enemyHealth);
                int max = Mathf.Max(1, (int)maxField.GetValue(enemyHealth));
                return Mathf.Clamp01((float)cur / max);
            }
            catch { }
        }

        return 1f;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Vẽ vùng sát thương đáp đất
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, damageRadius);
    }
#endif
}