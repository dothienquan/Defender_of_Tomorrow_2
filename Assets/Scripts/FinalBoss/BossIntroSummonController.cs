using System.Collections;
using UnityEngine;

/// <summary>
/// Boss flow:
/// 1) (Optional) Start dialogue
/// 2) After dialogue closes -> summon a "champion/minion" enemy to fight the player
/// 3) When the summoned enemy dies -> boss enters Main Phase (enable AI, take damage, etc.)
/// 
/// Requirements:
/// - DialogueUI + DialogueObject exist in your project (same as BossPhaseTransition)
/// - Summoned prefab should have EnemyHealth (to use OnDeath callback)
/// </summary>
public class BossIntroSummonController : MonoBehaviour
{
    public enum BossState
    {
        Idle,
        Dialogue,
        Summoning,
        WaitingSummonDeath,
        MainPhase
    }

    [Header("Dialogue (Intro)")]
    [SerializeField] private bool playDialogue = true;
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private DialogueObject introDialogue;
    [SerializeField] private string bossName = "???";

    [Header("Summon Enemy")]
    [SerializeField] private GameObject summonedEnemyPrefab;
    [SerializeField] private Transform summonSpawnPoint;
    [SerializeField] private GameObject summonVfxPrefab;
    [SerializeField] private float summonDelay = 0.35f;

    [Header("Boss Lock During Intro")]
    [Tooltip("Disable these behaviours while boss is waiting (ex: EnemyAI, EnemyPathfinding, skill scripts...)")]
    [SerializeField] private MonoBehaviour[] disableWhileLocked;
    [Tooltip("Boss health component. Will be set CanTakeDamage(false) while locked.")]
    [SerializeField] private EnemyHealth bossHealth;
    [Tooltip("Extra colliders to disable while locked (optional).")]
    [SerializeField] private Collider2D[] disableCollidersWhileLocked;

    [Header("Main Phase Enable")]
    [Tooltip("Enable these behaviours when Main Phase starts (if they were disabled in disableWhileLocked). Optional.")]
    [SerializeField] private MonoBehaviour[] enableOnMainPhase;
    [Tooltip("Call ResetHealthToMax() when main phase starts (useful if boss had 'fake' HP during intro).")]
    [SerializeField] private bool resetBossHealthOnMainPhase = false;

    [Header("Optional VFX")]
    [SerializeField] private GameObject mainPhaseVfxPrefab;

    public BossState State { get; private set; } = BossState.Idle;

    private bool started = false;
    private EnemyHealth summonedHealth;
    private GameObject summonedInstance;

    /// <summary>Call this once when player starts the encounter.</summary>
    public void StartEncounter()
    {
        if (started) return;
        started = true;
        StartCoroutine(EncounterRoutine());
    }

    private void Awake()
    {
        // Best-effort auto-find boss health on same object
        if (bossHealth == null) bossHealth = GetComponent<EnemyHealth>();
        LockBoss(true);
    }

    private IEnumerator EncounterRoutine()
    {
        // 1) Dialogue
        if (playDialogue && dialogueUI != null && introDialogue != null)
        {
            State = BossState.Dialogue;
            dialogueUI.Show(introDialogue, bossName);

            // Wait 1 frame (so UI can open)
            yield return null;

            // Wait until dialogue closes (same pattern you used)
            yield return new WaitUntil(() => dialogueUI.gameObject.activeSelf == false);
        }

        // 2) Summon
        State = BossState.Summoning;
        Vector3 spawnPos = summonSpawnPoint ? summonSpawnPoint.position : transform.position;

        GameObject vfx = null;
        if (summonVfxPrefab != null)
            vfx = Instantiate(summonVfxPrefab, spawnPos, Quaternion.identity);

        if (summonDelay > 0f)
            yield return new WaitForSeconds(summonDelay);

        if (summonedEnemyPrefab == null)
        {
            // No summoned enemy => go straight to main phase
            if (vfx != null) Destroy(vfx);
            EnterMainPhase();
            yield break;
        }

        summonedInstance = Instantiate(summonedEnemyPrefab, spawnPos, Quaternion.identity);

        if (vfx != null) Destroy(vfx);

        // 3) Wait summon death
        State = BossState.WaitingSummonDeath;

        summonedHealth = summonedInstance.GetComponent<EnemyHealth>();
        if (summonedHealth != null)
        {
            // Subscribe once
            summonedHealth.OnDeath += HandleSummonedDeath;
        }
        else
        {
            // Fallback: wait until object destroyed
            yield return new WaitUntil(() => summonedInstance == null);
            EnterMainPhase();
        }
    }

    private void HandleSummonedDeath()
    {
        // Unsubscribe to avoid leaks if something else references it
        if (summonedHealth != null)
            summonedHealth.OnDeath -= HandleSummonedDeath;

        EnterMainPhase();
    }

    private void EnterMainPhase()
    {
        if (State == BossState.MainPhase) return;

        State = BossState.MainPhase;

        if (mainPhaseVfxPrefab != null)
            Instantiate(mainPhaseVfxPrefab, transform.position, Quaternion.identity);

        if (resetBossHealthOnMainPhase && bossHealth != null)
            bossHealth.ResetHealthToMax();

        LockBoss(false);

        // Ensure explicitly enabled
        if (enableOnMainPhase != null)
        {
            for (int i = 0; i < enableOnMainPhase.Length; i++)
            {
                if (enableOnMainPhase[i] != null) enableOnMainPhase[i].enabled = true;
            }
        }
    }

    private void LockBoss(bool locked)
    {
        if (disableWhileLocked != null)
        {
            for (int i = 0; i < disableWhileLocked.Length; i++)
            {
                if (disableWhileLocked[i] != null)
                    disableWhileLocked[i].enabled = !locked;
            }
        }

        if (disableCollidersWhileLocked != null)
        {
            for (int i = 0; i < disableCollidersWhileLocked.Length; i++)
            {
                if (disableCollidersWhileLocked[i] != null)
                    disableCollidersWhileLocked[i].enabled = !locked;
            }
        }

        if (bossHealth != null)
            bossHealth.SetCanTakeDamage(!locked);
    }
}