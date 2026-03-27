using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio; // Cần thư viện này cho AudioMixer

public class PlayerController : Singleton<PlayerController>
{
    public bool FacingLeft { get { return facingLeft; } }

    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float dashSpeed = 4f;
    [SerializeField] private TrailRenderer myTrailRenderer;
    [SerializeField] private Transform weaponCollider;

    // --- NEW: AUDIO SETTINGS ---
    [Header("Audio Settings")]
    [Tooltip("Âm thanh bước chân")]
    [SerializeField] private AudioClip footstepSound;
    
    [Tooltip("Âm thanh khi lướt (Dash)")]
    [SerializeField] private AudioClip dashSound;

    [Tooltip("Gán SFX Mixer Group để chỉnh volume chung")]
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    [Header("Audio Params")]
    [Tooltip("Khoảng cách thời gian giữa các tiếng bước chân (giây). Số càng lớn tiếng càng thưa.")]
    [SerializeField] private float footstepInterval = 0.35f; 
    
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    private AudioSource audioSource;
    private float nextStepTime = 0f; // Biến canh thời gian bước chân tiếp theo
    // ---------------------------

    // WIND: vận tốc gió hiện tại
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
    private float dashCooldownTotal = 0.45f;

    protected override void Awake()
    {
        base.Awake();

        playerControls = new PlayerControls();
        rb = GetComponent<Rigidbody2D>();
        myAnimator = GetComponent<Animator>();
        mySpriteRender = GetComponent<SpriteRenderer>();
        knockback = GetComponent<Knockback>();

        // --- NEW: Setup AudioSource ---
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (sfxMixerGroup != null)
        {
            audioSource.outputAudioMixerGroup = sfxMixerGroup;
        }
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
        
        // --- NEW: Xử lý âm thanh bước chân ---
        HandleFootstepAudio();
    }

    private void FixedUpdate()
    {
        AdjustPlayerFacingDirection();
        Move();
    }

    // --- NEW: Hàm xử lý tiếng bước chân ---
    private void HandleFootstepAudio()
    {
        // 1. Kiểm tra xem có đang di chuyển không (magnitude > 0)
        // 2. Kiểm tra xem có đang Dash không (Dash thì dùng tiếng Dash riêng)
        // 3. Kiểm tra xem đã đến lúc phát tiếng tiếp theo chưa (Time.time >= nextStepTime)
        if (movement.sqrMagnitude > 0.01f && !isDashing)
        {
            if (Time.time >= nextStepTime)
            {
                PlaySound(footstepSound, true); // true = random pitch
                
                // Đặt thời gian cho bước tiếp theo
                nextStepTime = Time.time + footstepInterval;
            }
        }
    }

    // Hàm helper để phát âm thanh
    private void PlaySound(AudioClip clip, bool randomPitch = false)
    {
        if (clip == null || audioSource == null) return;

        if (randomPitch)
             audioSource.pitch = Random.Range(0.9f, 1.1f); // Thay đổi cao độ chút cho tự nhiên
        else
             audioSource.pitch = 1f;

        audioSource.PlayOneShot(clip, sfxVolume);
    }
    // --------------------------------------

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

        Vector2 inputVelocity = movement * moveSpeed;
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
            dashCooldownRemaining = dashCooldownTotal;
            moveSpeed *= dashSpeed;
            myTrailRenderer.emitting = true;
            
            // --- NEW: Phát tiếng Dash ---
            PlaySound(dashSound, true); 
            // ---------------------------

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

    public float GetDashCooldownRemaining()
    {
        return dashCooldownRemaining;
    }

    public float GetDashCooldownTotal()
    {
        return dashCooldownTotal;
    }

    public bool IsDashOnCooldown()
    {
        return dashCooldownRemaining > 0f;
    }

    public void SetWind(Vector2 wind)
    {
        currentWind = wind;
    }

    public void UpgradeMovementSpeed(float amount)
    {
        moveSpeed += amount;
        startingMoveSpeed += amount;
    }

    public float GetMovementSpeed()
    {
        return moveSpeed;
    }
}