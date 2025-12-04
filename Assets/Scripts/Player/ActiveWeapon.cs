using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActiveWeapon : Singleton<ActiveWeapon>
{
    public MonoBehaviour CurrentActiveWeapon { get; private set; }

    private PlayerControls playerControls;
    private float timeBetweenAttacks;

    private bool attackButtonDown, isAttacking = false;
    
    // Cooldown tracking for UI
    private float cooldownStartTime = 0f;
    private float currentCooldownDuration = 0f;
    
    /// <summary>
    /// Lấy thời gian cooldown còn lại (0 = hết cooldown)
    /// </summary>
    public float GetRemainingCooldown()
    {
        if (!isAttacking || currentCooldownDuration <= 0f)
        {
            return 0f;
        }
        
        float elapsed = Time.time - cooldownStartTime;
        float remaining = currentCooldownDuration - elapsed;
        return Mathf.Max(0f, remaining);
    }
    
    /// <summary>
    /// Lấy thời gian cooldown tối đa của weapon hiện tại
    /// </summary>
    public float GetMaxCooldown()
    {
        if (CurrentActiveWeapon == null)
        {
            return 0f;
        }
        
        IWeapon weapon = CurrentActiveWeapon as IWeapon;
        if (weapon == null)
        {
            return 0f;
        }
        
        WeaponInfo weaponInfo = weapon.GetWeaponInfo();
        if (weaponInfo == null)
        {
            return 0f;
        }
        
        return weaponInfo.weaponCooldown;
    }
    
    /// <summary>
    /// Kiểm tra xem weapon có đang trong cooldown không
    /// </summary>
    public bool IsOnCooldown()
    {
        return isAttacking && GetRemainingCooldown() > 0f;
    }

    protected override void Awake() {
        base.Awake();

        playerControls = new PlayerControls();
    }

    private void OnEnable()
    {
        if (playerControls != null)
        {
            playerControls.Enable();
        }
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

    private void Start()
    {
        playerControls.Combat.Attack.started += _ => StartAttacking();
        playerControls.Combat.Attack.canceled += _ => StopAttacking();

        AttackCooldown();
    }

    private void Update() {
        Attack();
    }

    public void NewWeapon(MonoBehaviour newWeapon) {
        CurrentActiveWeapon = newWeapon;

        AttackCooldown();
        timeBetweenAttacks = (CurrentActiveWeapon as IWeapon).GetWeaponInfo().weaponCooldown;
    }

    public void WeaponNull() {
        CurrentActiveWeapon = null;
    }

    private void AttackCooldown() {
        isAttacking = true;
        cooldownStartTime = Time.time;
        currentCooldownDuration = timeBetweenAttacks;
        StopAllCoroutines();
        StartCoroutine(TimeBetweenAttacksRoutine());
    }

    private IEnumerator TimeBetweenAttacksRoutine() {
        yield return new WaitForSeconds(timeBetweenAttacks);
        isAttacking = false;
        currentCooldownDuration = 0f;
    }

    private void StartAttacking()
    {
        attackButtonDown = true;
    }

    private void StopAttacking()
    {
        attackButtonDown = false;
    }

    private void Attack() {
        if (attackButtonDown && !isAttacking && CurrentActiveWeapon) {
            AttackCooldown();
            (CurrentActiveWeapon as IWeapon).Attack();
        }
    }
}
