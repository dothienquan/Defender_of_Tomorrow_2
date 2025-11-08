using System.Collections;
using UnityEngine;
using System.Reflection;

/// <summary>
/// Leap: Nhảy thẳng vào Player khi EnemyAI gọi IEnemy.Attack().
/// - Không sửa EnemyHealth. Tự đọc %HP nếu có (ưu tiên property HealthFraction),
///   nếu không có, thử lấy currentHP/maxHP bằng reflection an toàn.
/// - Khi HP < 50%, tốc độ nhảy tăng dần (HP càng thấp → nhảy càng nhanh).
/// </summary>
public class Leap : MonoBehaviour, IEnemy
{
    [Header("Leap")]
    [SerializeField] private float baseLeapSpeed = 12f;
    [Tooltip("Hệ số nhân tốc độ khi HP xuống 0% (tuyến tính từ 50% -> 0%). Ví dụ: 2 = gấp đôi ở 0% HP.")]
    [SerializeField] private float maxSpeedMultiplierAtLowHP = 2f;
    [SerializeField] private float stopDistance = 0.1f;

    [Header("Optional")]
    [SerializeField] private bool faceTargetOnAttack = true;
    [SerializeField] private string attackTriggerName = "Attack";

    private Transform target;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Component enemyHealth;          // Giữ reference nhưng không phụ thuộc kiểu cụ thể
    private Coroutine leapRoutine;

    private int attackHash;

    private void Awake()
    {
        // Lấy Player làm mục tiêu (cần PlayerController.Instance)
        target = PlayerController.Instance != null ? PlayerController.Instance.transform : null;

        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        enemyHealth = GetComponent(typeof(MonoBehaviour).Assembly.GetType("EnemyHealth")) ?? GetComponent("EnemyHealth");

        attackHash = !string.IsNullOrEmpty(attackTriggerName)
            ? Animator.StringToHash(attackTriggerName)
            : Animator.StringToHash("Attack");
    }

    public void Attack()
    {
        if (target == null) return;

        // Animation
        if (animator != null && attackHash != 0)
        {
            animator.SetTrigger(attackHash);
        }

        // Lật sprite theo hướng
        if (faceTargetOnAttack && spriteRenderer != null)
        {
            spriteRenderer.flipX = (transform.position.x - target.position.x) > 0f;
        }

        // Bắt đầu / khởi động lại cú nhảy
        if (leapRoutine != null) StopCoroutine(leapRoutine);
        leapRoutine = StartCoroutine(LeapTo(target.position));
    }

    private IEnumerator LeapTo(Vector3 targetPos)
    {
        targetPos.z = transform.position.z; // Giữ cùng Z (2D)

        while (Vector2.Distance(transform.position, targetPos) > stopDistance)
        {
            float speed = CurrentSpeed();
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
            yield return null;
        }
    }

    private float CurrentSpeed()
    {
        float frac = GetHealthFractionSafe();

        if (frac >= 0.5f) return baseLeapSpeed;

        // Khi HP < 50%: scale tuyến tính 1 -> maxSpeedMultiplierAtLowHP
        float t = (0.5f - frac) / 0.5f; // 0..1 cho 50% -> 0%
        float mult = Mathf.Lerp(1f, maxSpeedMultiplierAtLowHP, Mathf.Clamp01(t));
        return baseLeapSpeed * mult;
    }

    /// <summary>
    /// Thử lấy phần trăm máu hiện tại theo thứ tự:
    /// 1) Property "HealthFraction" (float 0..1)
    /// 2) Field "currentHP" và "maxHP" (int) qua reflection an toàn
    /// Nếu không có, mặc định 1f.
    /// </summary>
    private float GetHealthFractionSafe()
    {
        if (enemyHealth == null) return 1f;

        var type = enemyHealth.GetType();

        // Ưu tiên property HealthFraction (public hoặc nonpublic instance)
        var prop = type.GetProperty("HealthFraction", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (prop != null && prop.PropertyType == typeof(float))
        {
            try
            {
                object value = prop.GetValue(enemyHealth, null);
                return Mathf.Clamp01((float)value);
            }
            catch { /* bỏ qua và thử cách khác */ }
        }

        // Thử field currentHP / maxHP (int)
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
            catch { /* fallback */ }
        }

        return 1f;
    }
}