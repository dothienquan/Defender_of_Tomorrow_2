using UnityEngine;

public class EnemyAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Transform model;   // auto lấy từ Animator nếu để trống

    [System.Serializable]
    public class AttackHitbox
    {
        public string name;          // chỉ để dễ nhìn trong Inspector
        public Collider2D collider;  // hitbox của attack này
    }

    [Header("Attack Hitboxes (mỗi attack 1 hitbox)")]
    [SerializeField] private AttackHitbox[] attackHitboxes;

    private bool _isDead;

    private void Awake()
    {
        AutoAssignModelAndAnimator();
        InitHitboxes();
    }

    private void AutoAssignModelAndAnimator()
    {
        if (!animator)
            animator = GetComponentInChildren<Animator>();

        if (animator && model == null)
            model = animator.transform;

        if (!model)
            model = transform;
    }

    private void InitHitboxes()
    {
        if (attackHitboxes == null) return;

        // Tắt hết hitbox khi start
        for (int i = 0; i < attackHitboxes.Length; i++)
        {
            if (attackHitboxes[i].collider != null)
                attackHitboxes[i].collider.enabled = false;
        }
    }

    // ================== MOVE / FACING ==================
    public void SetMoveDirection(Vector2 dir)
    {
        if (_isDead || animator == null) return;

        float speedSqr = dir.sqrMagnitude;
        animator.SetFloat("Speed", speedSqr);
        animator.SetBool("IsMoving", speedSqr > 0.001f);

        if (speedSqr > 0.001f)
        {
            Vector2 n = dir.normalized;

            animator.SetFloat("MoveX", n.x);
            animator.SetFloat("MoveY", n.y);

            FlipModelByDirection(n.x);
        }
    }

    private void FlipModelByDirection(float xDir)
    {
        if (model == null) return;

        if (xDir < -0.01f)
        {
            model.localScale = new Vector3(-1, 1, 1);
        }
        else if (xDir > 0.01f)
        {
            model.localScale = new Vector3(1, 1, 1);
        }
        // colliders là con của model nên tự flip luôn
    }

    // ================== ATTACK ANIM ==================
    // Dùng nếu bạn chỉ có 1 kiểu attack, hoặc IEnemy tự set trigger riêng.
    public void PlayAttack()
    {
        animator.SetTrigger("Attack");
    }

    // Nếu bạn có nhiều animation tấn công khác nhau (Attack1, Attack2,...)
    // bạn có thể dùng hàm này:
    public void PlayAttackByIndex(int index)
    {
        animator.SetInteger("AttackIndex", index); // trong Animator phải có int AttackIndex
        animator.SetTrigger("Attack");
    }

    public void PlayDeath()
    {
        _isDead = true;
        animator.SetBool("IsDead", true);
    }

    // ================== HITBOX (Animation Event) ==================

    // Gọi từ Animation Event, với tham số int (0, 1, 2,...)
    public void EnableHitbox(int index)
    {
        DisableAllHitboxes();

        if (attackHitboxes == null) return;
        if (index < 0 || index >= attackHitboxes.Length) return;

        var col = attackHitboxes[index].collider;
        if (col != null)
            col.enabled = true;
    }

    // Nếu bạn muốn tắt 1 hitbox cụ thể (ít dùng, thường tắt tất cả)
    public void DisableHitbox(int index)
    {
        if (attackHitboxes == null) return;
        if (index < 0 || index >= attackHitboxes.Length) return;

        var col = attackHitboxes[index].collider;
        if (col != null)
            col.enabled = false;
    }

    // Gọi ở cuối animation attack (Animation Event) hoặc khi đổi state
    public void DisableAllHitboxes()
    {
        if (attackHitboxes == null) return;

        for (int i = 0; i < attackHitboxes.Length; i++)
        {
            if (attackHitboxes[i].collider != null)
                attackHitboxes[i].collider.enabled = false;
        }
    }

    // Tuỳ chọn: nếu muốn ép về Idle + tắt hitbox khi player rời AttackZone
    public void ForceIdle()
    {
        if (animator == null) return;

        animator.ResetTrigger("Attack");
        animator.SetBool("IsMoving", false);
        animator.SetFloat("Speed", 0f);

        DisableAllHitboxes();

        animator.Play("Idle", 0, 0f); // "Idle" = tên state idle trong Animator
    }
}
