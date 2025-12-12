using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
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

    [Header("On Death - NPC Spawn")]
    [SerializeField] private GameObject npcToActivate;  // NPC GameObject cần active khi boss chết
    [SerializeField] private float spawnDelay = 1f;  // Delay trước khi spawn NPC (sau khi boss chết)
    [SerializeField] private float fadeInDuration = 1f;  // Thời gian fade in
    [SerializeField] private float scaleUpDuration = 0.8f;  // Thời gian scale up
    [SerializeField] private Ease fadeInEase = Ease.OutQuad;  // Easing cho fade in
    [SerializeField] private Ease scaleUpEase = Ease.OutBack;  // Easing cho scale up (có bounce effect)

    [Header("Post NPC Spawn - Object Movement")]
    [SerializeField] private GameObject leftMovingObject;  // Object di chuyển sang trái
    [SerializeField] private GameObject rightMovingObject;  // Object di chuyển sang phải
    [SerializeField] private float postSpawnDelay = 7f;  // Delay sau khi NPC spawn xong (giây)
    [SerializeField] private float moveDistance = 10f;  // Khoảng cách di chuyển (units)
    [SerializeField] private float moveDuration = 2f;  // Thời gian di chuyển (giây)
    [SerializeField] private Ease moveEase = Ease.InOutQuad;  // Easing cho di chuyển

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
        // Subscribe vào OnDeath event của realHealth
        if (realHealth != null)
        {
            realHealth.OnDeath += OnBossDeath;
        }
        
        // Đảm bảo NPC bắt đầu inactive
        if (npcToActivate != null)
        {
            npcToActivate.SetActive(false);
        }
        
        // Spawn visuals
        SpawnVisuals();
    }
    
    private void OnDestroy()
    {
        // Unsubscribe để tránh memory leak
        if (realHealth != null)
        {
            realHealth.OnDeath -= OnBossDeath;
        }
        
        // Kill tất cả DOTween sequences để tránh lỗi khi object bị destroy
        if (realVisual != null) realVisual.transform.DOKill();
        if (fakeVisual != null) fakeVisual.transform.DOKill();
        if (npcToActivate != null) npcToActivate.transform.DOKill();
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
    
    // ======================
    //   BOSS DEATH HANDLER
    // ======================
    
    /// <summary>
    /// Được gọi khi boss chết (từ EnemyHealth.OnDeath event)
    /// </summary>
    private void OnBossDeath()
    {
        Debug.Log("[BossDuplicateManager] Boss died! Activating NPC with spawn effect...");
        
        if (npcToActivate == null)
        {
            Debug.LogWarning("[BossDuplicateManager] npcToActivate is not assigned!");
            return;
        }
        
        // Tạo một temporary GameObject để chạy coroutine (tránh bị dừng khi boss bị destroy)
        GameObject coroutineRunner = new GameObject("NPCSpawningCoroutineRunner");
        NPCSpawningHelper helper = coroutineRunner.AddComponent<NPCSpawningHelper>();
        
        // Gọi coroutine trên helper, truyền reference đến npcToActivate và objects để di chuyển
        helper.StartSpawnEffect(npcToActivate, spawnDelay, fadeInDuration, scaleUpDuration, fadeInEase, scaleUpEase,
            leftMovingObject, rightMovingObject, postSpawnDelay, moveDistance, moveDuration, moveEase);
    }
    
}

/// <summary>
/// Helper component để chạy spawn effect coroutine
/// Tránh bị dừng khi boss GameObject bị destroy
/// </summary>
public class NPCSpawningHelper : MonoBehaviour
{
    public void StartSpawnEffect(GameObject npc, float delay, float fadeDuration, float scaleDuration, DG.Tweening.Ease fadeEase, DG.Tweening.Ease scaleEase,
        GameObject leftObject, GameObject rightObject, float postDelay, float moveDist, float moveDur, DG.Tweening.Ease moveEase)
    {
        StartCoroutine(SpawnEffectCoroutine(npc, delay, fadeDuration, scaleDuration, fadeEase, scaleEase,
            leftObject, rightObject, postDelay, moveDist, moveDur, moveEase));
    }
    
    private System.Collections.IEnumerator SpawnEffectCoroutine(GameObject npc, float delay, float fadeDuration, float scaleDuration, DG.Tweening.Ease fadeEase, DG.Tweening.Ease scaleEase,
        GameObject leftObject, GameObject rightObject, float postDelay, float moveDist, float moveDur, DG.Tweening.Ease moveEase)
    {
        Debug.Log($"[NPCSpawningHelper] SpawnEffectCoroutine started. Delay: {delay}s, NPC: {(npc != null ? npc.name : "NULL")}");
        
        // Kiểm tra npc
        if (npc == null)
        {
            Debug.LogError("[NPCSpawningHelper] NPC GameObject is null!");
            Destroy(gameObject);
            yield break;
        }
        
        // Đợi delay trước khi spawn
        yield return new WaitForSeconds(delay);
        
        // Kiểm tra lại sau delay
        if (npc == null)
        {
            Debug.LogError("[NPCSpawningHelper] NPC GameObject became null after delay!");
            Destroy(gameObject);
            yield break;
        }
        
        Debug.Log($"[NPCSpawningHelper] Activating NPC: {npc.name}");
        
        // Active NPC (nhưng set alpha = 0 và scale = 0 để tạo hiệu ứng)
        npc.SetActive(true);
        
        // Kiểm tra xem NPC đã active chưa
        if (!npc.activeSelf)
        {
            Debug.LogError($"[NPCSpawningHelper] Failed to activate NPC: {npc.name}. Parent might be inactive!");
            Destroy(gameObject);
            yield break;
        }
        
        Debug.Log($"[NPCSpawningHelper] NPC activated successfully: {npc.name}");
        
        // Lấy SpriteRenderer hoặc CanvasGroup để làm fade in
        SpriteRenderer spriteRenderer = npc.GetComponent<SpriteRenderer>();
        CanvasGroup canvasGroup = npc.GetComponent<CanvasGroup>();
        
        Debug.Log($"[NPCSpawningHelper] SpriteRenderer: {(spriteRenderer != null ? "Found" : "Not found")}, CanvasGroup: {(canvasGroup != null ? "Found" : "Not found")}");
        
        // Lưu scale ban đầu
        Vector3 originalScale = npc.transform.localScale;
        Debug.Log($"[NPCSpawningHelper] Original scale: {originalScale}");
        
        // Set initial state: invisible và nhỏ
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            originalColor.a = 0f;
            spriteRenderer.color = originalColor;
            Debug.Log("[NPCSpawningHelper] Set SpriteRenderer alpha to 0");
        }
        else if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            Debug.Log("[NPCSpawningHelper] Set CanvasGroup alpha to 0");
        }
        else
        {
            Debug.LogWarning("[NPCSpawningHelper] No SpriteRenderer or CanvasGroup found! NPC will appear without fade effect.");
        }
        
        npc.transform.localScale = Vector3.zero;
        Debug.Log("[NPCSpawningHelper] Set scale to zero, starting animation...");
        
        // Tạo sequence cho hiệu ứng xuất hiện
        DG.Tweening.Sequence spawnSequence = DG.Tweening.DOTween.Sequence();
        
        // Fade in
        if (spriteRenderer != null)
        {
            spawnSequence.Append(spriteRenderer.DOFade(1f, fadeDuration).SetEase(fadeEase));
            Debug.Log("[NPCSpawningHelper] Added SpriteRenderer fade in to sequence");
        }
        else if (canvasGroup != null)
        {
            spawnSequence.Append(canvasGroup.DOFade(1f, fadeDuration).SetEase(fadeEase));
            Debug.Log("[NPCSpawningHelper] Added CanvasGroup fade in to sequence");
        }
        
        // Scale up (chạy đồng thời với fade in)
        spawnSequence.Join(npc.transform.DOScale(originalScale, scaleDuration).SetEase(scaleEase));
        Debug.Log("[NPCSpawningHelper] Added scale up to sequence");
        
        spawnSequence.OnComplete(() =>
        {
            Debug.Log($"[NPCSpawningHelper] NPC spawn effect completed! NPC active: {npc != null && npc.activeSelf}");
        });
        
        yield return spawnSequence.WaitForCompletion();
        
        Debug.Log("[NPCSpawningHelper] NPC spawn effect completed. Waiting for post-spawn delay...");
        
        // Đợi delay sau khi NPC spawn xong
        yield return new WaitForSeconds(postDelay);
        
        Debug.Log("[NPCSpawningHelper] Starting object movement sequence...");
        
        // Di chuyển 2 objects
        if (leftObject != null && rightObject != null)
        {
            // Lưu vị trí ban đầu
            Vector3 leftStartPos = leftObject.transform.position;
            Vector3 rightStartPos = rightObject.transform.position;
            
            // Tính toán vị trí đích
            Vector3 leftTargetPos = leftStartPos + Vector3.left * moveDist;  // Di chuyển sang trái
            Vector3 rightTargetPos = rightStartPos + Vector3.right * moveDist;  // Di chuyển sang phải
            
            // Tạo sequence di chuyển đồng thời
            DG.Tweening.Sequence moveSequence = DG.Tweening.DOTween.Sequence();
            
            // Di chuyển object bên trái sang trái
            moveSequence.Join(leftObject.transform.DOMove(leftTargetPos, moveDur).SetEase(moveEase));
            
            // Di chuyển object bên phải sang phải
            moveSequence.Join(rightObject.transform.DOMove(rightTargetPos, moveDur).SetEase(moveEase));
            
            moveSequence.OnComplete(() =>
            {
                Debug.Log("[NPCSpawningHelper] Object movement completed!");
            });
            
            yield return moveSequence.WaitForCompletion();
        }
        else
        {
            if (leftObject == null)
                Debug.LogWarning("[NPCSpawningHelper] Left moving object is null!");
            if (rightObject == null)
                Debug.LogWarning("[NPCSpawningHelper] Right moving object is null!");
        }
        
        Debug.Log("[NPCSpawningHelper] SpawnEffectCoroutine finished.");
        
        // Tự destroy helper GameObject sau khi hoàn thành
        Destroy(gameObject);
    }
}
