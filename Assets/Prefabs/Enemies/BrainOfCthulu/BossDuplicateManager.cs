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

    [Header("Fake Hit Effect (Phase 1)")]
    [SerializeField] private float fakeHitBlinkDuration = 2f;  // Thời gian nhấp nháy khi fake bị đánh
    [SerializeField] private float fakeHitBlinkInterval = 0.15f;  // Khoảng thời gian giữa mỗi lần nhấp nháy
    [SerializeField] private float shrinkScale = 0.3f;  // Kích thước khi thu nhỏ
    [SerializeField] private float shrinkDuration = 0.2f;  // Thời gian thu nhỏ
    [SerializeField] private float expandDuration = 0.2f;  // Thời gian phục hồi kích thước

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
            StartCoroutine(FakeHitBlinkThenSwap());
    }

    public void HandleRealDirectHit(int damage)
    {
        realHealth.TakeDamage(damage);

        // Kiểm tra phase switch nhưng KHÔNG dừng coroutine đang chạy
        // Chỉ đánh dấu để phase 2 được kích hoạt sau khi swap xong
        bool shouldEnterPhase2 = !isPhaseTwo && HPPercent() <= 0.5f;

        // Nếu đã ở phase 2, không cần swap nữa
        if (isPhaseTwo)
        {
            // Trigger flash on REAL VISUAL (phase 2 không swap)
            Flash flash = realVisual.GetComponent<Flash>();
            if (flash != null)
                StartCoroutine(flash.FlashRoutine());
            return;
        }

        // Phase 1: nhấp nháy với Flash và đổi chỗ
        if (swapOnHit)
        {
            StartCoroutine(RealHitBlinkThenSwap(shouldEnterPhase2));
        }
        else
        {
            // Nếu không swap, chỉ flash và kiểm tra phase switch
            Flash flash = realVisual.GetComponent<Flash>();
            if (flash != null)
                StartCoroutine(flash.FlashRoutine());
            
            if (shouldEnterPhase2)
            {
                CheckPhaseSwitch();
            }
        }
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

        // Ngay lập tức kích hoạt phase 2 khi HP <= 50%
        if (HPPercent() <= 0.5f)
        {
            // Dừng mọi tween đang chạy (nhưng không dừng coroutine để cho phép swap hoàn tất)
            if (realVisual != null) realVisual.transform.DOKill();
            if (fakeVisual != null) fakeVisual.transform.DOKill();
            
            // Reset hit animating flag để cho phép các coroutine khác chạy
            isHitAnimating = false;
            
            EnterPhaseTwo();
        }
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

    /// <summary>
    /// Khi fake body bị đánh ở phase 1: cả hai bản thể đứng yên và nhấp nháy trong 2 giây, sau đó đổi vị trí
    /// </summary>
    private IEnumerator FakeHitBlinkThenSwap()
    {
        if (isHitAnimating) yield break;
        isHitAnimating = true;

        // Dừng mọi movement nếu đang di chuyển
        if (realVisual != null)
        {
            realVisual.transform.DOKill();
            BossMover realMover = realVisual.GetComponent<BossMover>();
            if (realMover != null) realMover.enabled = false;
        }
        if (fakeVisual != null)
        {
            fakeVisual.transform.DOKill();
            BossMover fakeMover = fakeVisual.GetComponent<BossMover>();
            if (fakeMover != null) fakeMover.enabled = false;
        }

        // Lấy SpriteRenderer components
        SpriteRenderer realSR = realVisual != null ? realVisual.GetComponent<SpriteRenderer>() : null;
        SpriteRenderer fakeSR = fakeVisual != null ? fakeVisual.GetComponent<SpriteRenderer>() : null;

        if (realSR == null || fakeSR == null)
        {
            Debug.LogWarning("[BossDuplicateManager] SpriteRenderer not found on visuals!");
            isHitAnimating = false;
            yield break;
        }

        // Lưu màu gốc
        Color realOriginalColor = realSR.color;
        Color fakeOriginalColor = fakeSR.color;

        // Nhấp nháy cả hai bản thể trong 2 giây bằng cách thay đổi alpha
        float elapsed = 0f;
        int blinkCount = 0;
        int totalBlinks = Mathf.RoundToInt(fakeHitBlinkDuration / fakeHitBlinkInterval);

        while (elapsed < fakeHitBlinkDuration)
        {
            elapsed += fakeHitBlinkInterval;
            blinkCount++;
            bool isVisible = (blinkCount % 2 == 1); // Nhấp nháy: visible -> invisible -> visible...

            // Toggle alpha cho cả hai bản thể
            Color realColor = realOriginalColor;
            realColor.a = isVisible ? 1f : 0.3f;
            realSR.color = realColor;

            Color fakeColor = fakeOriginalColor;
            fakeColor.a = isVisible ? 1f : 0.3f;
            fakeSR.color = fakeColor;

            yield return new WaitForSeconds(fakeHitBlinkInterval);
        }

        // Khôi phục màu về bình thường
        realSR.color = realOriginalColor;
        fakeSR.color = fakeOriginalColor;

        // Thu nhỏ cả hai bản thể trước khi đổi chỗ
        Vector3 originalRealScale = realVisual.transform.localScale;
        Vector3 originalFakeScale = fakeVisual.transform.localScale;
        Vector3 shrinkVector = Vector3.one * shrinkScale;

        // Dừng mọi tween scale cũ
        realVisual.transform.DOKill();
        fakeVisual.transform.DOKill();

        // Thu nhỏ
        realVisual.transform.DOScale(shrinkVector, shrinkDuration).SetEase(Ease.InBack);
        fakeVisual.transform.DOScale(shrinkVector, shrinkDuration).SetEase(Ease.InBack);

        yield return new WaitForSeconds(shrinkDuration);

        // Đổi vị trí (trong khi đang nhỏ)
        SwapPositions();

        // Đợi swap hoàn tất
        yield return new WaitForSeconds(swapMoveDuration);

        // Phục hồi kích thước bình thường
        realVisual.transform.DOScale(originalRealScale, expandDuration).SetEase(Ease.OutBack);
        fakeVisual.transform.DOScale(originalFakeScale, expandDuration).SetEase(Ease.OutBack);

        yield return new WaitForSeconds(expandDuration);

        isHitAnimating = false;
    }

    /// <summary>
    /// Khi real body bị đánh ở phase 1: cả hai bản thể đứng yên, nhấp nháy với Flash component, sau đó đổi vị trí
    /// </summary>
    private IEnumerator RealHitBlinkThenSwap(bool shouldEnterPhase2AfterSwap = false)
    {
        if (isHitAnimating || isPhaseTwo) yield break;
        isHitAnimating = true;

        // Dừng mọi movement nếu đang di chuyển
        if (realVisual != null)
        {
            realVisual.transform.DOKill();
            BossMover realMover = realVisual.GetComponent<BossMover>();
            if (realMover != null) realMover.enabled = false;
        }
        if (fakeVisual != null)
        {
            fakeVisual.transform.DOKill();
            BossMover fakeMover = fakeVisual.GetComponent<BossMover>();
            if (fakeMover != null) fakeMover.enabled = false;
        }

        // Lấy Flash components
        Flash realFlash = realVisual != null ? realVisual.GetComponent<Flash>() : null;
        Flash fakeFlash = fakeVisual != null ? fakeVisual.GetComponent<Flash>() : null;

        if (realFlash == null)
        {
            Debug.LogWarning("[BossDuplicateManager] Flash component not found on real visual!");
            isHitAnimating = false;
            yield break;
        }

        // Nhấp nháy cả hai bản thể trong 2 giây bằng Flash component
        float elapsed = 0f;
        int blinkCount = 0;
        int totalBlinks = Mathf.RoundToInt(fakeHitBlinkDuration / fakeHitBlinkInterval);

        while (elapsed < fakeHitBlinkDuration && !isPhaseTwo)
        {
            elapsed += fakeHitBlinkInterval;
            blinkCount++;
            bool shouldFlash = (blinkCount % 2 == 1); // Flash mỗi lần blink

            if (shouldFlash)
            {
                // Flash real visual với màu mặc định (trắng)
                if (realFlash != null)
                    StartCoroutine(realFlash.FlashRoutine());
                
                // Flash fake visual cũng với màu trắng (hoặc có thể dùng màu khác)
                if (fakeFlash != null)
                    StartCoroutine(fakeFlash.FlashRoutine());
            }

            yield return new WaitForSeconds(fakeHitBlinkInterval);
        }

        // Nếu đã chuyển phase 2, không swap nữa
        if (isPhaseTwo)
        {
            isHitAnimating = false;
            yield break;
        }

        // Lưu scale ban đầu
        Vector3 originalRealScale = realVisual.transform.localScale;
        Vector3 originalFakeScale = fakeVisual.transform.localScale;
        Vector3 shrinkVector = Vector3.one * shrinkScale;

        // Dừng mọi tween scale cũ
        realVisual.transform.DOKill();
        fakeVisual.transform.DOKill();

        // Thu nhỏ cả hai bản thể trước khi đổi chỗ
        realVisual.transform.DOScale(shrinkVector, shrinkDuration).SetEase(Ease.InBack);
        fakeVisual.transform.DOScale(shrinkVector, shrinkDuration).SetEase(Ease.InBack);

        yield return new WaitForSeconds(shrinkDuration);

        // Đổi vị trí (trong khi đang nhỏ)
        SwapPositions();

        // Đợi swap hoàn tất
        yield return new WaitForSeconds(swapMoveDuration);

        // Phục hồi kích thước bình thường
        realVisual.transform.DOScale(originalRealScale, expandDuration).SetEase(Ease.OutBack);
        fakeVisual.transform.DOScale(originalFakeScale, expandDuration).SetEase(Ease.OutBack);

        yield return new WaitForSeconds(expandDuration);

        isHitAnimating = false;

        // Sau khi swap xong, nếu cần chuyển phase 2 thì kích hoạt
        if (shouldEnterPhase2AfterSwap && !isPhaseTwo)
        {
            CheckPhaseSwitch();
        }
    }

    // ======================
    //     SWAP POSITIONS
    // ======================

    private void SwapPositions()
    {
        if (realVisual == null || fakeVisual == null) return;

        Vector3 p1 = realVisual.transform.position;
        Vector3 p2 = fakeVisual.transform.position;

        // Lưu scale hiện tại trước khi kill tween (để không ảnh hưởng đến scale tween đang chạy)
        Vector3 realCurrentScale = realVisual.transform.localScale;
        Vector3 fakeCurrentScale = fakeVisual.transform.localScale;
        
        // Kill tất cả tween (bao gồm move) nhưng restore scale ngay lập tức
        realVisual.transform.DOKill();
        fakeVisual.transform.DOKill();
        
        // Restore scale ngay lập tức để không ảnh hưởng đến scale tween
        realVisual.transform.localScale = realCurrentScale;
        fakeVisual.transform.localScale = fakeCurrentScale;

        // Tween 2 bản thể trượt qua vị trí của nhau
        realVisual.transform.DOMove(p2, swapMoveDuration).SetEase(swapEase);
        fakeVisual.transform.DOMove(p1, swapMoveDuration).SetEase(swapEase);
    }
}
