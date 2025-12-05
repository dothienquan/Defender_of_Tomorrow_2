using UnityEngine;

public class EnemyAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Transform model; // sẽ auto tìm, không cần kéo nữa

    [Header("Attack")]
    [SerializeField] private Collider2D attackHitbox;

    private bool _isDead;
    private Vector3 _hitboxDefaultLocalPos;

    private void Awake()
    {
        AutoAssignModelAndAnimator();

        if (attackHitbox != null)
            _hitboxDefaultLocalPos = attackHitbox.transform.localPosition;
    }

    private void AutoAssignModelAndAnimator()
    {
        // Nếu animator chưa gán → tìm trong con
        if (!animator)
            animator = GetComponentInChildren<Animator>();

        // Nếu model chưa gán → lấy transform của object chứa Animator
        if (animator)
            model = animator.transform;

        // Nếu model vẫn không có (quá hiếm) → fallback lấy chính object này
        if (!model)
            model = transform;
    }

    public void SetMoveDirection(Vector2 dir)
    {
        if (_isDead) return;

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
            FlipHitbox(true);
        }
        else if (xDir > 0.01f)
        {
            model.localScale = new Vector3(1, 1, 1);
            FlipHitbox(false);
        }
    }

    private void FlipHitbox(bool faceLeft)
    {
        if (attackHitbox == null) return;

        float baseX = Mathf.Abs(_hitboxDefaultLocalPos.x);
        Vector3 pos = _hitboxDefaultLocalPos;
        pos.x = faceLeft ? -baseX : baseX;
        attackHitbox.transform.localPosition = pos;
    }

    public void PlayAttack() => animator.SetTrigger("Attack");

    public void PlayDeath()
    {
        _isDead = true;
        animator.SetBool("IsDead", true);
    }

    // Animation Events
    public void EnableHitbox()
    {
        if (attackHitbox != null)
            attackHitbox.enabled = true;
    }

    public void DisableHitbox()
    {
        if (attackHitbox != null)
            attackHitbox.enabled = false;
    }
}
