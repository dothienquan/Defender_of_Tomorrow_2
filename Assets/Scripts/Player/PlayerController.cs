using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : Singleton<PlayerController>
{
    public bool FacingLeft { get { return facingLeft; } }


    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float dashSpeed = 4f;
    [SerializeField] private TrailRenderer myTrailRenderer;
    [SerializeField] private Transform weaponCollider;

    // WIND: vận tốc gió hiện tại tác dụng lên player (world space, units/second)
    private Vector2 currentWind = Vector2.zero;

    private PlayerControls playerControls;
    private Vector2 movement;
    private Rigidbody2D rb;
    private Animator myAnimator;
    private SpriteRenderer mySpriteRender;
    private Knockback knockback;
    private float startingMoveSpeed;

    private bool facingLeft = false;
    private bool isDashing = false;
    private float dashCooldownRemaining = 0f;
    private float dashCooldownTotal = 0.45f; // dashTime (0.2) + dashCD (0.25)

    protected override void Awake()
    {
        base.Awake();

        playerControls = new PlayerControls();
        rb = GetComponent<Rigidbody2D>();
        myAnimator = GetComponent<Animator>();
        mySpriteRender = GetComponent<SpriteRenderer>();
        knockback = GetComponent<Knockback>();
    }

    private void Start()
    {
        playerControls.Combat.Dash.performed += _ => Dash();

        startingMoveSpeed = moveSpeed;

        ActiveInventory.Instance.EquipStartingWeapon();
    }

    private void OnEnable()
    {
        playerControls.Enable();
    }

    private void OnDisable()
    {
        if (playerControls != null)
        {
            playerControls.Disable();
        }
    }

    private void OnDestroy()
    {
        // Đảm bảo PlayerControls được disable trước khi destroy
        if (playerControls != null)
        {
            playerControls.Disable();
            playerControls.Dispose();
        }
    }

    private void Update()
    {
        PlayerInput();
        UpdateDashCooldown();
    }

    private void FixedUpdate()
    {
        AdjustPlayerFacingDirection();
        Move();
    }

    public Transform GetWeaponCollider()
    {
        return weaponCollider;
    }

    private void PlayerInput()
    {
        movement = playerControls.Movement.Move.ReadValue<Vector2>();

        myAnimator.SetFloat("moveX", movement.x);
        myAnimator.SetFloat("moveY", movement.y);
    }

    private void Move()
    {
        if (knockback.GettingKnockedBack || PlayerHealth.Instance.isDead) { return; }

        // vận tốc do input
        Vector2 inputVelocity = movement * moveSpeed;

        // tổng: input + gió
        Vector2 finalVelocity = inputVelocity + currentWind;

        rb.MovePosition(rb.position + finalVelocity * Time.fixedDeltaTime);
    }

    private void AdjustPlayerFacingDirection()
    {
        Vector3 mousePos = Input.mousePosition;
        Vector3 playerScreenPoint = Camera.main.WorldToScreenPoint(transform.position);

        if (mousePos.x < playerScreenPoint.x)
        {
            mySpriteRender.flipX = true;
            facingLeft = true;
        }
        else
        {
            mySpriteRender.flipX = false;
            facingLeft = false;
        }
    }

    private void Dash()
    {
        if (!isDashing && Stamina.Instance.CurrentStamina > 0 && dashCooldownRemaining <= 0f)
        {
            Stamina.Instance.UseStamina();
            isDashing = true;
            dashCooldownRemaining = dashCooldownTotal; // Bắt đầu cooldown
            moveSpeed *= dashSpeed;
            myTrailRenderer.emitting = true;
            StartCoroutine(EndDashRoutine());
        }
    }

    private IEnumerator EndDashRoutine()
    {
        float dashTime = .2f;
        float dashCD = .25f;
        yield return new WaitForSeconds(dashTime);
        moveSpeed = startingMoveSpeed;
        myTrailRenderer.emitting = false;
        yield return new WaitForSeconds(dashCD);
        isDashing = false;
    }

    /// <summary>
    /// Update dash cooldown timer
    /// </summary>
    private void UpdateDashCooldown()
    {
        if (dashCooldownRemaining > 0f)
        {
            dashCooldownRemaining -= Time.deltaTime;
            if (dashCooldownRemaining < 0f)
            {
                dashCooldownRemaining = 0f;
            }
        }
    }

    /// <summary>
    /// Lấy thời gian cooldown còn lại của dash
    /// </summary>
    public float GetDashCooldownRemaining()
    {
        return dashCooldownRemaining;
    }

    /// <summary>
    /// Lấy tổng thời gian cooldown của dash
    /// </summary>
    public float GetDashCooldownTotal()
    {
        return dashCooldownTotal;
    }

    /// <summary>
    /// Kiểm tra dash có đang trong cooldown không
    /// </summary>
    public bool IsDashOnCooldown()
    {
        return dashCooldownRemaining > 0f;
    }

    // WIND: hàm cho vùng gió gọi vào
    public void SetWind(Vector2 wind)
    {
        currentWind = wind;
    }

    /// <summary>
    /// Nâng cấp movement speed (dùng cho upgrade system)
    /// </summary>
    public void UpgradeMovementSpeed(float amount)
    {
        moveSpeed += amount;
        startingMoveSpeed += amount; // Cập nhật cả starting speed để dash vẫn đúng
    }

    /// <summary>
    /// Lấy movement speed hiện tại
    /// </summary>
    public float GetMovementSpeed()
    {
        return moveSpeed;
    }
}
