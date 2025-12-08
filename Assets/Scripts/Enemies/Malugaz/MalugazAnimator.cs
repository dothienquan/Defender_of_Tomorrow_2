using UnityEngine;

[DisallowMultipleComponent]
public class MalugazAnimator : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform model;   // auto lấy từ Animator nếu để trống

    [System.Serializable]
    public class AttackHitbox
    {
        public string name;          // chỉ để dễ nhìn trong Inspector
        public Collider2D collider;  // hitbox của attack (slam, charge, combo, ...)
    }

    [Header("Attack Hitboxes (mỗi attack 1 hitbox)")]
    [SerializeField] private AttackHitbox[] attackHitboxes;

    [Header("Animator Parameters")]
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string isMovingParam = "IsMoving";
    [SerializeField] private string moveXParam = "MoveX";
    [SerializeField] private string moveYParam = "MoveY";
    [SerializeField] private string isDeadParam = "IsDead";

    [Header("Triggers / Int for boss")]
    [SerializeField] private string phase1Trigger = "Phase1";
    [SerializeField] private string phase2Trigger = "Phase2";
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private string attackIndexParam = "AttackIndex";
    [SerializeField] private string slamTrigger = "Slam";
    [SerializeField] private string chargeTrigger = "Charge";
    [SerializeField] private string teleportTrigger = "Teleport";

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

        for (int i = 0; i < attackHitboxes.Length; i++)
        {
            if (attackHitboxes[i].collider != null)
                attackHitboxes[i].collider.enabled = false;
        }
    }

    // ================== PHASE CONTROL ==================

    public void PlayPhase1Intro()
    {
        if (_isDead || animator == null) return;
        if (!string.IsNullOrEmpty(phase1Trigger))
            animator.SetTrigger(phase1Trigger);
    }

    public void PlayPhase2Transform()
    {
        if (_isDead || animator == null) return;
        if (!string.IsNullOrEmpty(phase2Trigger))
            animator.SetTrigger(phase2Trigger);
    }

    public void PlayTeleport()
    {
        if (_isDead || animator == null) return;
        if (!string.IsNullOrEmpty(teleportTrigger))
            animator.SetTrigger(teleportTrigger);
    }

    // ================== MOVE / FACING ==================

    public void SetMoveDirection(Vector2 dir)
    {
        if (_isDead || animator == null) return;

        float speedSqr = dir.sqrMagnitude;

        animator.SetFloat(speedParam, speedSqr);
        animator.SetBool(isMovingParam, speedSqr > 0.001f);

        if (speedSqr > 0.001f)
        {
            Vector2 n = dir.normalized;
            animator.SetFloat(moveXParam, n.x);
            animator.SetFloat(moveYParam, n.y);
            FlipModelByDirection(n.x);
        }
    }

    public void ForceIdleFront()
    {
        if (_isDead || animator == null) return;

        animator.SetBool(isMovingParam, false);
        animator.SetFloat(speedParam, 0f);
        animator.SetFloat(moveXParam, 0f);
        animator.SetFloat(moveYParam, -1f); // front
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

    public void PlayAttack()
    {
        if (_isDead || animator == null) return;
        if (!string.IsNullOrEmpty(attackTrigger))
            animator.SetTrigger(attackTrigger);
    }

    public void PlayAttackByIndex(int idx)
    {
        if (_isDead || animator == null) return;

        if (!string.IsNullOrEmpty(attackIndexParam))
            animator.SetInteger(attackIndexParam, idx);

        if (!string.IsNullOrEmpty(attackTrigger))
            animator.SetTrigger(attackTrigger);
    }

    public void PlaySlam()
    {
        if (_isDead || animator == null) return;
        if (!string.IsNullOrEmpty(slamTrigger))
            animator.SetTrigger(slamTrigger);
    }

    public void PlayCharge()
    {
        if (_isDead || animator == null) return;
        if (!string.IsNullOrEmpty(chargeTrigger))
            animator.SetTrigger(chargeTrigger);
    }

    public void PlayDeath()
    {
        _isDead = true;
        DisableAllHitboxes();

        if (animator != null && !string.IsNullOrEmpty(isDeadParam))
            animator.SetBool(isDeadParam, true);
    }

    // ================== HITBOX (Animation Event) ==================

    public void EnableHitbox(int index)
    {
        DisableAllHitboxes();

        if (attackHitboxes == null) return;
        if (index < 0 || index >= attackHitboxes.Length) return;

        var col = attackHitboxes[index].collider;
        if (col != null)
            col.enabled = true;
    }

    public void DisableHitbox(int index)
    {
        if (attackHitboxes == null) return;
        if (index < 0 || index >= attackHitboxes.Length) return;

        var col = attackHitboxes[index].collider;
        if (col != null)
            col.enabled = false;
    }

    public void DisableAllHitboxes()
    {
        if (attackHitboxes == null) return;

        for (int i = 0; i < attackHitboxes.Length; i++)
        {
            if (attackHitboxes[i].collider != null)
                attackHitboxes[i].collider.enabled = false;
        }
    }
}
