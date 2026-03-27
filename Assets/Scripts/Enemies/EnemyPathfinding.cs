using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyPathfinding : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;

    private Rigidbody2D rb;
    private Vector2 moveDir;
    private Knockback knockback;
    private SpriteRenderer spriteRenderer;

    // Slow system
    private struct SlowEffect
    {
        public float multiplier;
        public float endTime;
        public SlowEffect(float m, float e) { multiplier = m; endTime = e; }
    }
    private readonly List<SlowEffect> slows = new List<SlowEffect>();
    private float CurrentSlowMultiplier
    {
        get
        {
            if (slows.Count == 0) return 1f;
            float m = 1f;
            // Lấy multiplier nhỏ nhất (chậm nhất) còn hiệu lực
            float now = Time.time;
            for (int i = slows.Count - 1; i >= 0; i--)
            {
                if (now >= slows[i].endTime) { slows.RemoveAt(i); continue; }
                m = Mathf.Min(m, slows[i].multiplier);
            }
            return m;
        }
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        knockback = GetComponent<Knockback>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (knockback.GettingKnockedBack) { return; }

        float finalSpeed = moveSpeed * CurrentSlowMultiplier;
        rb.MovePosition(rb.position + moveDir * (finalSpeed * Time.fixedDeltaTime));

        // Nếu dùng EnemyAnimator để flip sprite thì có thể xoá đoạn này
        if (moveDir.x < 0)
        {
            spriteRenderer.flipX = true;
        }
        else if (moveDir.x > 0)
        {
            spriteRenderer.flipX = false;
        }
    }

    public void MoveTo(Vector2 targetDirection)
    {
        // giả định đã normalized từ AI
        moveDir = targetDirection;
    }

    public void StopMoving()
    {
        moveDir = Vector3.zero;
    }

    // --- Slow API ---
    public void ApplySlow(float multiplier, float duration, bool allowStack)
    {
        multiplier = Mathf.Clamp(multiplier, 0.05f, 1f);
        duration = Mathf.Max(0f, duration);
        float end = Time.time + duration;

        if (!allowStack)
        {
            // Không cộng dồn: nếu có slow cũ, lấy effect mạnh hơn và gia hạn
            float minM = multiplier;
            float maxEnd = end;
            float now = Time.time;
            for (int i = slows.Count - 1; i >= 0; i--)
            {
                if (now >= slows[i].endTime) { slows.RemoveAt(i); continue; }
                minM = Mathf.Min(minM, slows[i].multiplier);
                maxEnd = Mathf.Max(maxEnd, slows[i].endTime);
            }
            slows.Clear();
            slows.Add(new SlowEffect(minM, maxEnd));
        }
        else
        {
            slows.Add(new SlowEffect(multiplier, end));
        }
    }

    public float GetMoveSpeed() => moveSpeed;
    public void SetMoveSpeed(float newSpeed) => moveSpeed = Mathf.Max(0f, newSpeed);

    // ✅ HÀM THÊM CHO ANIMATOR
    public Vector2 GetCurrentVelocity()
    {
        return moveDir;
    }
}
