using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FlyingEnemyController : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float chaseDistance = 6f;
    public float attackDistance = 1.5f;

    [Header("Attack")]
    public float attackCooldown = 1.2f;
    public int[] attackPattern = new int[] { 1, 2, 3 };   // combo lặp

    private int _attackStep = 0;
    private float _attackTimer;

    private Rigidbody2D _rb;
    private Animator _anim;
    private bool _isDead;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponent<Animator>();
    }

    private void Update()
    {
        if (_isDead || player == null) return;

        _attackTimer -= Time.deltaTime;

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= attackDistance)
        {
            _rb.linearVelocity = Vector2.zero;
            SetMoveAnim(false);
            TryAttack();
        }
        else if (dist <= chaseDistance)
        {
            MoveTowards(player.position);
            SetMoveAnim(true);
        }
        else
        {
            _rb.linearVelocity = Vector2.zero;
            SetIdleAnim();
        }

        HandleFlip();
    }

    private void MoveTowards(Vector2 targetPos)
    {
        Vector2 dir = (targetPos - (Vector2)transform.position).normalized;
        _rb.linearVelocity = dir * moveSpeed;
    }

    private void SetMoveAnim(bool isMoving)
    {
        _anim.SetBool("isFlying", isMoving);
        _anim.SetBool("idle", !isMoving);
    }

    private void SetIdleAnim()
    {
        _anim.SetBool("isFlying", false);
        _anim.SetBool("idle", true);
    }

    private void TryAttack()
    {
        if (_attackTimer > 0f) return;
        if (attackPattern == null || attackPattern.Length == 0) return;

        _attackTimer = attackCooldown;

        // Lấy kiểu attack tiếp theo trong pattern
        int atk = attackPattern[_attackStep];

        _attackStep++;
        if (_attackStep >= attackPattern.Length)
            _attackStep = 0;

        PlayAttack(atk);
    }

    private void PlayAttack(int attackIndex)
    {
        // Tắt hết những attack khác
        _anim.SetBool("attack1", false);
        _anim.SetBool("attack2", false);
        _anim.SetBool("attack3", false);

        // Bật đúng animation
        switch (attackIndex)
        {
            case 1:
                _anim.SetBool("attack1", true);
                break;
            case 2:
                _anim.SetBool("attack2", true);
                break;
            case 3:
                _anim.SetBool("attack3", true);
                break;
        }
    }

    private void HandleFlip()
    {
        if (player == null) return;

        var scale = transform.localScale;
        if (player.position.x > transform.position.x)
            scale.x = Mathf.Abs(scale.x);
        else
            scale.x = -Mathf.Abs(scale.x);

        transform.localScale = scale;
    }

    public void Die()
    {
        if (_isDead) return;

        _isDead = true;
        _rb.linearVelocity = Vector2.zero;

        // Tắt mọi bool
        _anim.SetBool("attack1", false);
        _anim.SetBool("attack2", false);
        _anim.SetBool("attack3", false);
        _anim.SetBool("isFlying", false);
        _anim.SetBool("idle", false);

        _anim.SetBool("Die", true); // Animator của bạn dùng bool Die
    }
}
