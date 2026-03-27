using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(SpriteRenderer))]
public class BossHitRedirector : MonoBehaviour
{
    public BossDuplicateManager manager;
    public bool isReal = false;

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

    public void HandleProjectileHit(
        Projectile projectile,
        int directDamage, bool isDirect,
        int dotDmg, float dotInterval, float dotDuration, bool dotStackable,
        bool applySlow, float slowMult, float slowDur, bool slowStackable,
        GameObject hitVFX)
    {
        // Fake visual hit
        if (!isReal)
        {
            PlayFakeBlink();

            if (hitVFX) Instantiate(hitVFX, transform.position, Quaternion.identity);

            manager.OnFakeHit();
            return;
        }

        // REAL visual hit
        if (isDirect)
            manager.HandleRealDirectHit(directDamage);
        else
            manager.HandleRealDotHit(dotDmg, dotInterval, dotDuration, dotStackable);

        if (applySlow)
            manager.HandleRealSlow(slowMult, slowDur, slowStackable);

        if (hitVFX) Instantiate(hitVFX, transform.position, Quaternion.identity);
    }

    private void PlayFakeBlink()
    {
        sr.DOKill();
        Color faded = new Color(originalColor.r, originalColor.g, originalColor.b, fakeAlpha);

        sr.DOColor(faded, blinkTime).OnComplete(() =>
        {
            sr.DOColor(originalColor, blinkTime);
        });
    }
}
