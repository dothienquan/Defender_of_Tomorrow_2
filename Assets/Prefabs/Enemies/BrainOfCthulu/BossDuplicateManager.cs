using System.Collections;
using System.Reflection;
using UnityEngine;
using DG.Tweening;

public class BossDuplicateManager : MonoBehaviour
{
    [Header("Visual Prefab (NO EnemyHealth, only SpriteRenderer + Collider2D + BossHitRedirector)")]
    public GameObject visualPrefab;

    [Header("Phase 1 Static Positions")]
    public Vector2 offsetLeft = new Vector2(-2f, 0f);
    public Vector2 offsetRight = new Vector2(2f, 0f);

    [Header("Phase 2 Movement Area (Local Offset)")]
    public Vector2 areaCenter = Vector2.zero;
    public Vector2 areaSize = new Vector2(6f, 3f);

    [Header("Phase 2 Movement Settings")]
    public float moveSpeed = 1.8f;
    public float idleTimeBetweenMoves = 0.6f;

    [Header("General Settings")]
    public bool swapOnHit = true;

    [Header("Swap FX")]
    [SerializeField] private float swapMoveDuration = 0.25f;   // thời gian 2 bản thể trượt qua nhau
    [SerializeField] private Ease swapEase = Ease.InOutQuad;   // easing cho cảm giác mượt

    private GameObject realVisual;
    private GameObject fakeVisual;

    private BossHitRedirector realRedirector;
    private BossHitRedirector fakeRedirector;

    private EnemyHealth realHealth;

    private FieldInfo fi_currentHealth;
    private FieldInfo fi_startingHealth;

    private bool isPhaseTwo = false;

    // Hiệu ứng boss bị đánh (tránh spam)
    private bool isHitAnimating = false;

    private void Awake()
    {
        realHealth = GetComponent<EnemyHealth>();
        if (!realHealth)
        {
            Debug.LogError("BossDuplicateManager requires EnemyHealth on the same GameObject.");
            enabled = false;
            return;
        }

        // Reflection for HP reading
        var t = typeof(EnemyHealth);
        fi_currentHealth = t.GetField("currentHealth", BindingFlags.NonPublic | BindingFlags.Instance);
        fi_startingHealth = t.GetField("startingHealth", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    private void Start()
    {
        SpawnVisuals();
    }

    private void SpawnVisuals()
    {
        if (!visualPrefab)
        {
            Debug.LogError("BossDuplicateManager: visualPrefab is missing!");
            return;
        }

        // REAL visual instance
        realVisual = Instantiate(
            visualPrefab,
            transform.position + (Vector3)offsetLeft,
            Quaternion.identity,
            transform
        );

        // FAKE visual instance
        fakeVisual = Instantiate(
            visualPrefab,
            transform.position + (Vector3)offsetRight,
            Quaternion.identity,
            transform
        );

        // Redirectors
        realRedirector = realVisual.GetComponent<BossHitRedirector>();
        fakeRedirector = fakeVisual.GetComponent<BossHitRedirector>();

        realRedirector.manager = this;
        fakeRedirector.manager = this;

        realRedirector.isReal = true;
        fakeRedirector.isReal = false;

        // Movement components (disabled initially)
        SetupMover(realVisual);
        SetupMover(fakeVisual);
    }

    private void SetupMover(GameObject g)
    {
        BossMover mover = g.GetComponent<BossMover>();
        if (!mover)
            mover = g.AddComponent<BossMover>();

        mover.enabled = false;
        mover.SetArea(() => (Vector2)transform.position + areaCenter, areaSize);
        mover.SetSpeed(moveSpeed);
        mover.SetIdleTime(idleTimeBetweenMoves);
    }

    // ============================
    //   CALLED BY HIT REDIRECTOR
    // ============================

    public void OnFakeHit()
    {
        if (!isPhaseTwo && swapOnHit)
            StartCoroutine(HitEffectThenSwap());
    }

    public void HandleRealDirectHit(int damage)
    {
        realHealth.TakeDamage(damage);

        // Trigger flash on REAL VISUAL
        Flash flash = realVisual.GetComponent<Flash>();
        if (flash != null)
            StartCoroutine(flash.FlashRoutine());

        CheckPhaseSwitch();

        if (!isPhaseTwo && swapOnHit)
            StartCoroutine(HitEffectThenSwap());
    }

    public void HandleRealDotHit(int dmg, float interval, float duration, bool stack)
    {
        realHealth.ApplyDot(dmg, interval, duration, stack);

        Flash flash = realVisual.GetComponent<Flash>();
        if (flash != null)
            StartCoroutine(flash.FlashRoutine(new Color(1f, 0.5f, 0f))); // DoT flash color

        CheckPhaseSwitch();
    }

    public void HandleRealSlow(float mult, float dur, bool stack)
    {
        realHealth.ApplySlow(mult, dur, stack);

        Flash flash = realVisual.GetComponent<Flash>();
        if (flash != null)
            StartCoroutine(flash.FlashRoutine(new Color(0.3f, 0.6f, 1f))); // slow flash color
    }

    // ======================
    //     PHASE LOGIC
    // ======================

    private float HPPercent()
    {
        if (fi_currentHealth != null && fi_startingHealth != null)
        {
            int cur = (int)fi_currentHealth.GetValue(realHealth);
            int max = (int)fi_startingHealth.GetValue(realHealth);
            if (max <= 0) return 1f;
            return (float)cur / max;
        }
        return 1f;
    }

    private void CheckPhaseSwitch()
    {
        if (isPhaseTwo) return;

        if (HPPercent() <= 0.5f)
            EnterPhaseTwo();
    }

    private void EnterPhaseTwo()
    {
        isPhaseTwo = true;

        realVisual.GetComponent<BossMover>().enabled = true;
        fakeVisual.GetComponent<BossMover>().enabled = true;

        PlaceInArea(realVisual.transform);
        PlaceInArea(fakeVisual.transform);
    }

    private void PlaceInArea(Transform t)
    {
        Vector2 center = (Vector2)transform.position + areaCenter;
        Vector2 half = areaSize * 0.5f;

        t.position = center + new Vector2(
            UnityEngine.Random.Range(-half.x, half.x),
            UnityEngine.Random.Range(-half.y, half.y)
        );
    }

    // ======================
    //   HIT FX + SWAP
    // ======================

    private IEnumerator HitVanishRoutine()
    {
        if (isHitAnimating) yield break;
        isHitAnimating = true;

        // Thu nhỏ và "biến mất"
        if (realVisual != null)
            realVisual.transform.DOScale(Vector3.zero, 0.25f);
        if (fakeVisual != null)
            fakeVisual.transform.DOScale(Vector3.zero, 0.25f);

        yield return new WaitForSeconds(1f);

        // Scale lên lại
        if (realVisual != null)
            realVisual.transform.DOScale(Vector3.one, 0.25f);
        if (fakeVisual != null)
            fakeVisual.transform.DOScale(Vector3.one, 0.25f);

        yield return new WaitForSeconds(0.25f);

        isHitAnimating = false;
    }

    private IEnumerator HitEffectThenSwap()
    {
        yield return HitVanishRoutine();
        SwapPositions();
    }

    // ======================
    //     SWAP POSITIONS
    // ======================

    private void SwapPositions()
    {
        if (realVisual == null || fakeVisual == null) return;

        Vector3 p1 = realVisual.transform.position;
        Vector3 p2 = fakeVisual.transform.position;

        // Dừng tween cũ nếu có
        realVisual.transform.DOKill();
        fakeVisual.transform.DOKill();

        // Tween 2 bản thể trượt qua vị trí của nhau
        realVisual.transform.DOMove(p2, swapMoveDuration).SetEase(swapEase);
        fakeVisual.transform.DOMove(p1, swapMoveDuration).SetEase(swapEase);
    }
}
