using System;
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(SpriteRenderer))]
public class BossHitRedirector : MonoBehaviour
{
    [Tooltip("Reference to the manager that owns this boss pair.")]
    public BossDuplicateManager manager;

    [Tooltip("Is this the 'real' instance (the one that has the EnemyHealth component)?")]
    public bool isReal = false;

    // Transparent blink config
    [Header("Fake Blink")]
    public float fakeAlpha = 0.35f;
    public float blinkTime = 0.12f;

    private SpriteRenderer sr;
    private Color originalColor;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;
    }

    /// <summary>
    /// Called by Projectile when it collides with this visual.
    /// </summary>
    public void HandleProjectileHit(Projectile projectile,
        int directDamage, bool isDirect,
        int dotDamagePerTick, float dotTickInterval, float dotDuration, bool dotStackable,
        bool applySlowOnHit, float slowMultiplier, float slowDuration, bool slowStackable,
        GameObject hitVFXPrefab)
    {
        if (manager == null)
        {
            Debug.LogWarning("BossHitRedirector: manager not set on " + gameObject.name);
            // fallback: if real, try to find EnemyHealth on same GO
            if (isReal)
            {
                var eh = GetComponent<EnemyHealth>();
                if (eh != null)
                {
                    if (isDirect) eh.TakeDamage(directDamage);
                    else eh.ApplyDot(dotDamagePerTick, dotTickInterval, dotDuration, dotStackable);

                    if (applySlowOnHit) eh.ApplySlow(slowMultiplier, slowDuration, slowStackable);
                }
            }
            if (hitVFXPrefab) Instantiate(hitVFXPrefab, transform.position, transform.rotation);
            return;
        }

        if (!isReal)
        {
            // fake: do blink + maybe spawn hit VFX, but DO NOT apply damage
            PlayFakeBlink();
            if (hitVFXPrefab) Instantiate(hitVFXPrefab, transform.position, transform.rotation);

            // Inform manager that a fake was hit (so it may swap in Phase 1).
            manager.OnFakeHit();
            return;
        }

        // this is the real: forward damage & slow & DoT to manager which will apply to the real EnemyHealth
        if (isDirect)
            manager.HandleRealDirectHit(directDamage);
        else
            manager.HandleRealDotHit(dotDamagePerTick, dotTickInterval, dotDuration, dotStackable);

        if (applySlowOnHit)
            manager.HandleRealSlow(slowMultiplier, slowDuration, slowStackable);

        if (hitVFXPrefab) Instantiate(hitVFXPrefab, transform.position, transform.rotation);
    }

    private void PlayFakeBlink()
    {
        // DOTween simple alpha blink
        sr.DOKill();
        Color toColor = new Color(originalColor.r, originalColor.g, originalColor.b, fakeAlpha);
        sr.DOColor(toColor, blinkTime).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            sr.DOColor(originalColor, blinkTime).SetEase(Ease.InQuad);
        });
    }
}
