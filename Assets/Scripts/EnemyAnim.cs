using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAnim : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;

    private static readonly int MoveX = Animator.StringToHash("moveX");
    private static readonly int MoveY = Animator.StringToHash("moveY");
    private static readonly int LastX = Animator.StringToHash("LastX");
    private static readonly int LastY = Animator.StringToHash("LastY");
    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int Attack = Animator.StringToHash("Attack");
    private static readonly int Hurt = Animator.StringToHash("Hurt");
    private static readonly int IsDead = Animator.StringToHash("IsDead");

    private bool _isDead;

    private void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (!rb) rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (_isDead) return;

        Vector2 v = rb.linearVelocity;
        float speed = v.magnitude;
        animator.SetFloat(Speed, speed);

        if (speed > 0.001f)
        {
            Vector2 dir = v.normalized;
            animator.SetFloat(MoveX, dir.x);
            // Đảo ngược moveY để fix animation run trên/dưới bị ngược
            animator.SetFloat(MoveY, -dir.y);
            animator.SetFloat(LastX, dir.x);
            // Đảo ngược LastY để đồng bộ với moveY
            animator.SetFloat(LastY, -dir.y);
        }
    }

    public void PlayAttack()
    {
        if (_isDead || animator.GetBool(IsDead)) return;
        animator.SetTrigger(Attack);
    }

    public void PlayHurt()
    {
        if (_isDead || animator.GetBool(IsDead)) return;
        animator.SetTrigger(Hurt);
    }

    public void Die()
    {
        if (_isDead) return;
        _isDead = true;

        animator.SetBool(IsDead, true);

        // Tìm death animation clip và disable animator sau khi chạy xong
        StartCoroutine(StopAnimatorAfterDeath());
    }

    private IEnumerator StopAnimatorAfterDeath()
    {
        // Đợi một frame để animator chuyển sang Death state
        yield return null;

        // Tìm death animation clip trong animator
        float deathAnimDuration = 1f; // Default fallback
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            RuntimeAnimatorController ac = animator.runtimeAnimatorController;
            foreach (AnimationClip clip in ac.animationClips)
            {
                if (clip.name.Contains("Death") || clip.name.Contains("death"))
                {
                    deathAnimDuration = clip.length;
                    break;
                }
            }
        }

        // Đợi death animation chạy xong một lần
        yield return new WaitForSeconds(deathAnimDuration);

        // Dừng animator để tránh loop
        if (animator != null)
        {
            animator.enabled = false;
        }
    }
}
