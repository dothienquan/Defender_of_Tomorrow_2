using System;
using System.Reflection;
using UnityEngine;

public class BossDuplicateManager : MonoBehaviour
{
    [Header("Visual Prefab (NO EnemyHealth, has SpriteRenderer + Collider + BossHitRedirector)")]
    public GameObject visualPrefab;

    [Header("Phase 1 Static Positions")]
    public Vector2 offsetLeft = new Vector2(-2f, 0f);
    public Vector2 offsetRight = new Vector2(2f, 0f);

    [Header("Phase 2 Movement Area (local offset from root)")]
    public Vector2 areaCenter = Vector2.zero;
    public Vector2 areaSize = new Vector2(6f, 3f);

    [Header("Phase 2 Movement Settings")]
    public float moveSpeed = 1.8f;
    public float idleTimeBetweenMoves = 0.6f;

    [Header("General Settings")]
    public bool swapOnHit = true;

    private GameObject realVisual;
    private GameObject fakeVisual;

    private BossHitRedirector realRedirector;
    private BossHitRedirector fakeRedirector;

    private EnemyHealth realHealth;

    private FieldInfo fi_currentHealth;
    private FieldInfo fi_startingHealth;

    private bool isPhaseTwo = false;

    private void Awake()
    {
        realHealth = GetComponent<EnemyHealth>();
        if (realHealth == null)
        {
            Debug.LogError("BossDuplicateManager requires EnemyHealth on root.");
            enabled = false;
            return;
        }

        // Reflection for health % check
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
        if (visualPrefab == null)
        {
            Debug.LogError("BossDuplicateManager: visualPrefab not assigned.");
            return;
        }

        // REAL visual
        realVisual = Instantiate(
            visualPrefab,
            transform.position + (Vector3)offsetLeft,
            Quaternion.identity,
            transform);

        // FAKE visual
        fakeVisual = Instantiate(
            visualPrefab,
            transform.position + (Vector3)offsetRight,
            Quaternion.identity,
            transform);

        // Assign redirectors
        realRedirector = realVisual.GetComponent<BossHitRedirector>();
        fakeRedirector = fakeVisual.GetComponent<BossHitRedirector>();

        if (!realRedirector || !fakeRedirector)
        {
            Debug.LogError("visualPrefab must contain BossHitRedirector.");
            return;
        }

        realRedirector.manager = this;
        fakeRedirector.manager = this;

        realRedirector.isReal = true;
        fakeRedirector.isReal = false;

        // Add movers for phase 2
        SetupMover(realVisual);
        SetupMover(fakeVisual);
    }

    private void SetupMover(GameObject visual)
    {
        BossMover mover = visual.GetComponent<BossMover>();
        if (!mover) mover = visual.AddComponent<BossMover>();

        mover.enabled = false;
        mover.SetArea(
            () => (Vector2)transform.position + areaCenter,
            areaSize);
        mover.SetSpeed(moveSpeed);
        mover.SetIdleTime(idleTimeBetweenMoves);
    }

    // ===== HIT REDIRECTION EVENTS =====

    public void OnFakeHit()
    {
        if (!isPhaseTwo && swapOnHit)
            SwapPositions();
    }

    public void HandleRealDirectHit(int damage)
    {
        realHealth.TakeDamage(damage);
        CheckPhaseSwitch();
        if (!isPhaseTwo && swapOnHit)
            SwapPositions();
    }

    public void HandleRealDotHit(int dmgPerTick, float interval, float duration, bool stackable)
    {
        realHealth.ApplyDot(dmgPerTick, interval, duration, stackable);
        CheckPhaseSwitch();
    }

    public void HandleRealSlow(float multiplier, float duration, bool stackable)
    {
        realHealth.ApplySlow(multiplier, duration, stackable);
    }

    // ===== SWAPPING =====

    private void SwapPositions()
    {
        Vector3 a = realVisual.transform.position;
        Vector3 b = fakeVisual.transform.position;

        // 50% chance real <-> fake
        if (UnityEngine.Random.value < 0.5f)
        {
            realVisual.transform.position = b;
            fakeVisual.transform.position = a;
        }
        else
        {
            // Always swap, but keeps randomness
            realVisual.transform.position = b;
            fakeVisual.transform.position = a;
        }
    }

    // ===== PHASE SWITCH =====

    private float GetHealthPercent()
    {
        if (fi_currentHealth != null && fi_startingHealth != null)
        {
            int cur = (int)fi_currentHealth.GetValue(realHealth);
            int max = (int)fi_startingHealth.GetValue(realHealth);
            if (max <= 0) return 1f;

            return (float)cur / max;
        }

        return 1f; // fallback
    }

    private void CheckPhaseSwitch()
    {
        if (isPhaseTwo) return;

        if (GetHealthPercent() <= 0.5f)
            EnterPhaseTwo();
    }

    private void EnterPhaseTwo()
    {
        isPhaseTwo = true;

        // Enable movement scripts
        realVisual.GetComponent<BossMover>().enabled = true;
        fakeVisual.GetComponent<BossMover>().enabled = true;

        // Reposition both inside area
        PlaceInArea(realVisual.transform);
        PlaceInArea(fakeVisual.transform);
    }

    private void PlaceInArea(Transform t)
    {
        Vector2 center = (Vector2)transform.position + areaCenter;
        Vector2 half = areaSize * 0.5f;

        t.position = center + new Vector2(
            UnityEngine.Random.Range(-half.x, half.x),
            UnityEngine.Random.Range(-half.y, half.y));
    }
}
