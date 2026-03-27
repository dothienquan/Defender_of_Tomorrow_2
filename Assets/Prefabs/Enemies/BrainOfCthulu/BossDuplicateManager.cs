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

    [Header("Phase 1 - Real Hit VFX")]
    [SerializeField] private GameObject realHitVFXPrefab;  // VFX spawn khi đánh trúng bản thể thật

    [Header("Phase 1 - Fake Hit Beam")]
    [SerializeField] private bool useBeamPrefab = false;  // Nếu true, dùng beamPrefab. Nếu false, tạo LineRenderer động
    [SerializeField] private GameObject beamPrefab;  // Prefab beam/ray (tùy chọn)
    [SerializeField] private Material beamMaterial;  // Material cho LineRenderer (để tạo hiệu ứng sấm sét)
    [SerializeField] private float beamDuration = 0.3f;  // Thời gian beam tồn tại
    [SerializeField] private float beamWidth = 0.2f;  // Độ rộng của beam
    [SerializeField] private Color beamColor = Color.white;  // Màu của beam
    [SerializeField] private int beamPoints = 10;  // Số điểm để tạo hiệu ứng zigzag (sấm sét)
    [SerializeField] private float beamZigzagAmount = 0.3f;  // Độ lệch zigzag
    [SerializeField] private int beamDamage = 1;  // Sát thương của beam
    [SerializeField] private bool enableBeamCollider = true;  // Bật collider để gây damage
    [SerializeField] private float beamExpandTime = 0.1f;  // Thời gian tia mở rộng từ đầu đến cuối (nhanh hơn)
    [SerializeField] private float beamBlinkInterval = 0.05f;  // Khoảng thời gian giữa các lần nhấp nháy
    [SerializeField] private float beamBlinkMinAlpha = 0.6f;  // Alpha tối thiểu khi nhấp nháy
    [SerializeField] private float beamBlinkMaxAlpha = 1f;  // Alpha tối đa khi nhấp nháy

    [Header("Phase 2 - Circular Shooting")]
    [SerializeField] private GameObject projectilePrefab;  // Prefab projectile để bắn ở phase 2
    [SerializeField] private int phase2ProjectileCount = 8;  // Số lượng projectile trong pattern vòng tròn
    [SerializeField] private float phase2ProjectileSpeed = 5f;  // Tốc độ projectile phase 2
    [SerializeField] private float phase2ShootInterval = 2f;  // Khoảng thời gian giữa các lần bắn
    [SerializeField] private float phase2ProjectileSpawnDistance = 0.5f;  // Khoảng cách spawn từ boss

    [Header("On Death - NPC Spawn")]
    [SerializeField] private GameObject npcToActivate;  // NPC GameObject cần active khi boss chết
    [SerializeField] private float spawnDelay = 1f;  // Delay trước khi spawn NPC (sau khi boss chết)
    [SerializeField] private GameObject npcToActivate2;  // NPC GameObject thứ 2 cần active khi boss chết
    [SerializeField] private float spawnDelay2 = 2f;  // Delay trước khi spawn NPC thứ 2 (sau khi boss chết)
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

    // Phase 2 shooting
    private Coroutine phase2ShootingCoroutine;

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
        if (npcToActivate2 != null)
        {
            npcToActivate2.SetActive(false);
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
        
        // Dừng shooting coroutine
        if (phase2ShootingCoroutine != null)
        {
            StopCoroutine(phase2ShootingCoroutine);
        }
        
        // Kill tất cả DOTween sequences để tránh lỗi khi object bị destroy
        if (realVisual != null) realVisual.transform.DOKill();
        if (fakeVisual != null) fakeVisual.transform.DOKill();
        if (npcToActivate != null) npcToActivate.transform.DOKill();
        if (npcToActivate2 != null) npcToActivate2.transform.DOKill();
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

        // Kiểm tra phase switch
        bool shouldEnterPhase2 = !isPhaseTwo && HPPercent() <= 0.5f;

        // Lấy Flash component một lần
        Flash flash = realVisual != null ? realVisual.GetComponent<Flash>() : null;

        // Nếu đã ở phase 2, chỉ flash
        if (isPhaseTwo)
        {
            if (flash != null)
                StartCoroutine(flash.FlashRoutine());
            return;
        }

        // Phase 1: Spawn VFX, flash, và swap
        if (swapOnHit)
        {
            StartCoroutine(RealHitVFXThenSwap(shouldEnterPhase2));
        }
        else
        {
            // Nếu không swap, chỉ spawn VFX và flash
            if (realHitVFXPrefab != null)
            {
                GameObject vfxInstance = Instantiate(realHitVFXPrefab, realVisual.transform.position, Quaternion.identity);
                // Auto-destroy VFX sau khi hoàn thành
                ParticleSystem particles = vfxInstance.GetComponent<ParticleSystem>();
                if (particles != null)
                {
                    if (!particles.main.loop)
                    {
                        float duration = particles.main.duration + particles.main.startLifetime.constantMax;
                        Destroy(vfxInstance, duration);
                    }
                }
                else
                {
                    // Fallback: destroy sau 5 giây nếu không có ParticleSystem
                    Destroy(vfxInstance, 5f);
                }
            }

            // Flash effect
            if (flash != null)
                StartCoroutine(flash.FlashRoutine());

            // Kiểm tra phase switch
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

        // Bắt đầu circular shooting pattern
        if (phase2ShootingCoroutine != null)
            StopCoroutine(phase2ShootingCoroutine);
        phase2ShootingCoroutine = StartCoroutine(Phase2CircularShooting());
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
    /// Khi fake body bị đánh ở phase 1: bắn projectile về player, nhấp nháy, sau đó đổi vị trí
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

        // Bắn tia về player
        if (PlayerController.Instance != null)
        {
            Vector3 playerPos = PlayerController.Instance.transform.position;
            Vector3 shootPosition = fakeVisual != null ? fakeVisual.transform.position : transform.position;
            Vector2 directionToPlayer = (playerPos - shootPosition).normalized;
            float distance = Vector2.Distance(shootPosition, playerPos);

            GameObject beam = null;
            LineRenderer lr = null;

            // Nếu dùng prefab
            if (useBeamPrefab && beamPrefab != null)
            {
                beam = Instantiate(beamPrefab, shootPosition, Quaternion.identity);
                beam.transform.right = directionToPlayer;
                lr = beam.GetComponent<LineRenderer>();
            }
            else
            {
                // Tạo LineRenderer động
                beam = new GameObject("BossBeam");
                beam.transform.position = shootPosition;
                beam.transform.right = directionToPlayer;
                
                lr = beam.AddComponent<LineRenderer>();
                lr.material = beamMaterial != null ? beamMaterial : new Material(Shader.Find("Sprites/Default"));
                lr.startColor = beamColor;
                lr.endColor = beamColor;
                lr.startWidth = beamWidth;
                lr.endWidth = beamWidth;
                lr.useWorldSpace = true;
                lr.sortingOrder = 10;  // Đảm bảo hiển thị trên các sprite khác
            }

            // Tạo hiệu ứng sấm sét (zigzag) nếu beamPoints > 2
            if (lr != null)
            {
                Vector3 start = shootPosition;
                Vector3 end = playerPos;
                Vector3 direction = (end - start).normalized;
                Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0f);
                
                // Tính toán tất cả các điểm trước
                Vector3[] allPoints;
                int totalPoints;
                
                if (beamPoints > 2)
                {
                    // Tạo đường zigzag để giống sấm sét
                    totalPoints = beamPoints;
                    allPoints = new Vector3[totalPoints];
                    
                    for (int i = 0; i < totalPoints; i++)
                    {
                        float t = (float)i / (totalPoints - 1);
                        Vector3 basePos = Vector3.Lerp(start, end, t);
                        
                        // Thêm zigzag ngẫu nhiên
                        float zigzag = Mathf.Sin(t * Mathf.PI * 3f) * beamZigzagAmount * (1f - Mathf.Abs(t - 0.5f) * 2f);
                        Vector3 offset = perpendicular * zigzag;
                        
                        allPoints[i] = basePos + offset;
                    }
                }
                else
                {
                    // Đường thẳng đơn giản
                    totalPoints = 2;
                    allPoints = new Vector3[] { start, end };
                }

                // Thêm component để animate việc mở rộng tia
                BeamExpander expander = beam.AddComponent<BeamExpander>();
                expander.Initialize(lr, allPoints, beamExpandTime, beamWidth, enableBeamCollider, beamDamage, transform,
                    beamBlinkInterval, beamBlinkMinAlpha, beamBlinkMaxAlpha, beamDuration);
            }
            else
            {
                // Nếu dùng SpriteRenderer (như MagicLaser), scale theo khoảng cách
                SpriteRenderer sr = beam.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.size = new Vector2(distance, beamWidth);
                }
            }

            // Tự động destroy beam sau thời gian cố định (không phụ thuộc vào việc hit player)
            // Beam sẽ tồn tại đúng beamDuration giây
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
    /// Khi real body bị đánh ở phase 1: spawn VFX, flash, sau đó đổi vị trí (KHÔNG bắn projectile)
    /// </summary>
    private IEnumerator RealHitVFXThenSwap(bool shouldEnterPhase2AfterSwap = false)
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

        // Spawn VFX
        if (realHitVFXPrefab != null)
        {
            GameObject vfxInstance = Instantiate(realHitVFXPrefab, realVisual.transform.position, Quaternion.identity);
            // Auto-destroy VFX sau khi hoàn thành
            ParticleSystem particles = vfxInstance.GetComponent<ParticleSystem>();
            if (particles != null)
            {
                if (!particles.main.loop)
                {
                    float duration = particles.main.duration + particles.main.startLifetime.constantMax;
                    Destroy(vfxInstance, duration);
                }
            }
            else
            {
                // Fallback: destroy sau 5 giây nếu không có ParticleSystem
                Destroy(vfxInstance, 5f);
            }
        }

        // Flash effect
        Flash flash = realVisual != null ? realVisual.GetComponent<Flash>() : null;
        if (flash != null)
            StartCoroutine(flash.FlashRoutine());

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
    //   PHASE 2 CIRCULAR SHOOTING
    // ======================

    /// <summary>
    /// Bắn đạn theo pattern vòng tròn ở phase 2
    /// </summary>
    private IEnumerator Phase2CircularShooting()
    {
        while (isPhaseTwo && realHealth != null && realHealth.GetCurrentHealth() > 0)
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning("[BossDuplicateManager] Projectile prefab is missing for phase 2 shooting!");
                yield break;
            }

            // Bắn từ vị trí của real visual (hoặc transform nếu realVisual null)
            Vector3 shootPosition = realVisual != null ? realVisual.transform.position : transform.position;

            // Tính toán các hướng chia đều 360 độ
            float angleStep = 360f / phase2ProjectileCount;

            // Bắn các projectile
            for (int i = 0; i < phase2ProjectileCount; i++)
            {
                // Tính góc (bắt đầu từ hướng lên - 90 độ)
                float angle = (i * angleStep - 90f) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                Vector3 spawnPos = shootPosition + (Vector3)(direction * phase2ProjectileSpawnDistance);

                GameObject projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
                projectile.transform.right = direction;

                if (projectile.TryGetComponent(out Projectile proj))
                {
                    proj.UpdateMoveSpeed(phase2ProjectileSpeed);
                    proj.SetIsEnemyProjectile(true);
                }
            }

            // Đợi trước khi bắn lần tiếp theo
            yield return new WaitForSeconds(phase2ShootInterval);
        }
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
        
        // Spawn NPC đầu tiên (có movement sau khi spawn)
        if (npcToActivate != null)
        {
            GameObject coroutineRunner1 = new GameObject("NPCSpawningCoroutineRunner1");
            NPCSpawningHelper helper1 = coroutineRunner1.AddComponent<NPCSpawningHelper>();
            
            helper1.StartSpawnEffect(npcToActivate, spawnDelay, fadeInDuration, scaleUpDuration, fadeInEase, scaleUpEase,
                leftMovingObject, rightMovingObject, postSpawnDelay, moveDistance, moveDuration, moveEase);
        }
        else
        {
            Debug.LogWarning("[BossDuplicateManager] npcToActivate is not assigned!");
        }
        
        // Spawn NPC thứ 2 (chỉ spawn effect, không có movement)
        if (npcToActivate2 != null)
        {
            GameObject coroutineRunner2 = new GameObject("NPCSpawningCoroutineRunner2");
            NPCSpawningHelper helper2 = coroutineRunner2.AddComponent<NPCSpawningHelper>();
            
            helper2.StartSpawnEffectSimple(npcToActivate2, spawnDelay2, fadeInDuration, scaleUpDuration, fadeInEase, scaleUpEase);
        }
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
    
    public void StartSpawnEffectSimple(GameObject npc, float delay, float fadeDuration, float scaleDuration, DG.Tweening.Ease fadeEase, DG.Tweening.Ease scaleEase)
    {
        StartCoroutine(SpawnEffectSimpleCoroutine(npc, delay, fadeDuration, scaleDuration, fadeEase, scaleEase));
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
    
    private System.Collections.IEnumerator SpawnEffectSimpleCoroutine(GameObject npc, float delay, float fadeDuration, float scaleDuration, DG.Tweening.Ease fadeEase, DG.Tweening.Ease scaleEase)
    {
        Debug.Log($"[NPCSpawningHelper] SpawnEffectSimpleCoroutine started. Delay: {delay}s, NPC: {(npc != null ? npc.name : "NULL")}");
        
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
        
        Debug.Log("[NPCSpawningHelper] SpawnEffectSimpleCoroutine finished.");
        
        // Tự destroy helper GameObject sau khi hoàn thành
        Destroy(gameObject);
    }
}

/// <summary>
/// Component để animate việc mở rộng tia từ đầu đến cuối
/// </summary>
public class BeamExpander : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private Vector3[] allPoints;
    private float expandTime;
    private float beamWidth;
    private bool enableCollider;
    private int damage;
    private Transform attacker;
    private EdgeCollider2D edgeCollider;
    private BeamDamageDealer damageDealer;
    private float blinkInterval;
    private float blinkMinAlpha;
    private float blinkMaxAlpha;
    private float totalDuration;
    private Color originalStartColor;
    private Color originalEndColor;

    public void Initialize(LineRenderer lr, Vector3[] points, float time, float width, bool enableCol, int dmg, Transform attackerTransform,
        float blinkInt, float blinkMin, float blinkMax, float duration)
    {
        lineRenderer = lr;
        allPoints = points;
        expandTime = time;
        beamWidth = width;
        enableCollider = enableCol;
        damage = dmg;
        attacker = attackerTransform;
        blinkInterval = blinkInt;
        blinkMinAlpha = blinkMin;
        blinkMaxAlpha = blinkMax;
        totalDuration = duration;

        // Lưu màu gốc
        originalStartColor = lineRenderer.startColor;
        originalEndColor = lineRenderer.endColor;

        // Khởi tạo với 0 điểm
        lineRenderer.positionCount = 0;

        // Thêm collider nếu cần
        if (enableCollider)
        {
            edgeCollider = gameObject.AddComponent<EdgeCollider2D>();
            edgeCollider.isTrigger = true;
            edgeCollider.edgeRadius = beamWidth * 0.5f;
            
            damageDealer = gameObject.AddComponent<BeamDamageDealer>();
            damageDealer.Initialize(damage, attacker);
        }

        // Bắt đầu animation
        StartCoroutine(ExpandBeamCoroutine());
    }

    private IEnumerator ExpandBeamCoroutine()
    {
        float elapsed = 0f;
        int totalPoints = allPoints.Length;
        float blinkTimer = 0f;
        bool isBlinking = false;

        // Phase 1: Mở rộng tia
        while (elapsed < expandTime)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / expandTime);
            
            // Tính số điểm hiện tại dựa trên progress
            int currentPointCount = Mathf.CeilToInt(totalPoints * progress);
            if (currentPointCount < 1) currentPointCount = 1;
            if (currentPointCount > totalPoints) currentPointCount = totalPoints;

            // Cập nhật LineRenderer
            lineRenderer.positionCount = currentPointCount;
            for (int i = 0; i < currentPointCount; i++)
            {
                lineRenderer.SetPosition(i, allPoints[i]);
            }

            // Cập nhật EdgeCollider2D
            if (enableCollider && edgeCollider != null && currentPointCount > 1)
            {
                Vector2[] colliderPoints = new Vector2[currentPointCount];
                for (int i = 0; i < currentPointCount; i++)
                {
                    colliderPoints[i] = transform.InverseTransformPoint(allPoints[i]);
                }
                edgeCollider.points = colliderPoints;
            }

            yield return null;
        }

        // Đảm bảo tia đã mở rộng hoàn toàn
        lineRenderer.positionCount = totalPoints;
        for (int i = 0; i < totalPoints; i++)
        {
            lineRenderer.SetPosition(i, allPoints[i]);
        }

        // Cập nhật collider cuối cùng
        if (enableCollider && edgeCollider != null)
        {
            Vector2[] finalColliderPoints = new Vector2[totalPoints];
            for (int i = 0; i < totalPoints; i++)
            {
                finalColliderPoints[i] = transform.InverseTransformPoint(allPoints[i]);
            }
            edgeCollider.points = finalColliderPoints;
        }

        // Phase 2: Nhấp nháy và tồn tại
        float remainingTime = totalDuration - expandTime;
        elapsed = 0f;

        while (elapsed < remainingTime)
        {
            elapsed += Time.deltaTime;
            blinkTimer += Time.deltaTime;

            // Nhấp nháy
            if (blinkTimer >= blinkInterval)
            {
                blinkTimer = 0f;
                isBlinking = !isBlinking;

                // Thay đổi alpha
                float alpha = isBlinking ? blinkMinAlpha : blinkMaxAlpha;
                Color startColor = originalStartColor;
                Color endColor = originalEndColor;
                startColor.a = alpha;
                endColor.a = alpha;
                lineRenderer.startColor = startColor;
                lineRenderer.endColor = endColor;
            }

            yield return null;
        }

        // Khôi phục màu trước khi destroy
        lineRenderer.startColor = originalStartColor;
        lineRenderer.endColor = originalEndColor;

        // Destroy beam sau thời gian cố định (không phụ thuộc vào việc hit player)
        Destroy(gameObject);
    }
}

/// <summary>
/// Component để beam gây damage cho player
/// </summary>
public class BeamDamageDealer : MonoBehaviour
{
    private int damage;
    private Transform attacker;
    private bool hasHitPlayer = false;  // Tránh hit nhiều lần trong cùng một frame

    public void Initialize(int dmg, Transform attackerTransform)
    {
        damage = dmg;
        attacker = attackerTransform;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHitPlayer) return;
        if (!other.CompareTag("Player")) return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null) return;

        // Gây damage cho player
        playerHealth.TakeDamage(damage, attacker != null ? attacker : transform);
        hasHitPlayer = true;  // Đánh dấu đã hit để tránh hit nhiều lần
    }
}
