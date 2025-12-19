using System.Collections;
using UnityEngine;

/// <summary>
/// Phase0 flow:
/// Player enters trigger -> Dialog 1 -> Summon enemy (with VFX) -> wait enemy death -> Dialog 2 ->
/// (same frame) deactivate Phase0 + activate Phase1.
/// 
/// This script integrates with your NPCDialogue/DialogueUI usage pattern: DialogueUI.Show(...)
/// and waits for DialogueUI GameObject activeSelf to go true then false (dialog closed).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BossPhase0Sequence : MonoBehaviour
{
    [Header("Dialogue (uses DialogueUI.Show like NPCDialogue)")]
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private DialogueObject dialog1;
    [SerializeField] private DialogueObject dialog2;
    [SerializeField] private string bossName = "Boss";
    [SerializeField] private Sprite bossAvatar;

    [Header("Summon")]
    [SerializeField] private GameObject summonedEnemyPrefab;
    [SerializeField] private Transform summonSpawnPoint;
    [SerializeField] private GameObject summonVfxPrefab;
    [SerializeField] private float summonDelay = 0.35f;

    [Header("Phase Switch (same frame)")]
    [Tooltip("These objects will be activated when phase 0 ends (enable all in same frame).")]
    [SerializeField] private GameObject[] phase1ObjectsToActivate;

    [Tooltip("These objects will be deactivated when phase 0 ends (disable all in same frame).")]
    [SerializeField] private GameObject[] phase0ObjectsToDeactivate;

    [Header("Trigger Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool runOnce = true;

    private bool _started;
    private Collider2D _trigger;

    private void Awake()
    {
        _trigger = GetComponent<Collider2D>();
        if (_trigger != null) _trigger.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (runOnce && _started) return;

        _started = true;
        StartCoroutine(Flow());
    }

    private IEnumerator Flow()
    {
        if (dialogueUI == null)
        {
            Debug.LogError($"[{name}] dialogueUI is NULL - cannot show dialogues.", this);
            yield break;
        }
        if (summonedEnemyPrefab == null)
        {
            Debug.LogError($"[{name}] summonedEnemyPrefab is NULL - cannot summon.", this);
            yield break;
        }

        // -------- Dialog 1 --------
        if (dialog1 != null)
        {
            dialogueUI.Show(dialog1, bossName, bossAvatar);
            yield return WaitDialogueClose();
        }

        // -------- Summon (VFX -> enemy) --------
        Vector3 pos = summonSpawnPoint != null ? summonSpawnPoint.position : transform.position;

        GameObject vfx = null;
        if (summonVfxPrefab != null)
            vfx = Instantiate(summonVfxPrefab, pos, Quaternion.identity);

        if (summonDelay > 0f)
            yield return new WaitForSeconds(summonDelay);

        var enemy = Instantiate(summonedEnemyPrefab, pos, Quaternion.identity);

        if (vfx != null) Destroy(vfx);

        // -------- Wait enemy death --------
        yield return WaitEnemyDeath(enemy);

        // -------- Dialog 2 --------
        if (dialog2 != null)
        {
            dialogueUI.Show(dialog2, bossName, bossAvatar);
            yield return WaitDialogueClose();
        }

        // -------- Switch phase (same frame) --------
        ActivatePhase1DeactivatePhase0SameFrame();
    }

    private IEnumerator WaitDialogueClose()
    {
        // Wait until dialogue UI becomes active (if Show activates it next frame)
        int safety = 0;
        while (!dialogueUI.gameObject.activeSelf && safety < 10)
        {
            safety++;
            yield return null;
        }

        // Wait until dialogue UI closes
        while (dialogueUI.gameObject.activeSelf)
            yield return null;
    }

    private IEnumerator WaitEnemyDeath(GameObject enemy)
    {
        if (enemy == null) yield break;

        bool died = false;

        // Prefer EnemyHealth.OnDeath if present
        var health = enemy.GetComponentInChildren<EnemyHealth>();
        if (health != null)
        {
            void OnDead() => died = true;
            health.OnDeath += OnDead;

            while (!died && enemy != null)
                yield return null;

            health.OnDeath -= OnDead;
        }
        else
        {
            // Fallback: wait until object destroyed
            while (enemy != null)
                yield return null;
        }
    }

    private void ActivatePhase1DeactivatePhase0SameFrame()
    {
        // Enable Phase 1 objects first
        if (phase1ObjectsToActivate != null)
        {
            for (int i = 0; i < phase1ObjectsToActivate.Length; i++)
            {
                var go = phase1ObjectsToActivate[i];
                if (go != null) go.SetActive(true);
            }
        }

        // Then disable Phase 0 objects
        if (phase0ObjectsToDeactivate != null)
        {
            for (int i = 0; i < phase0ObjectsToDeactivate.Length; i++)
            {
                var go = phase0ObjectsToDeactivate[i];
                if (go != null) go.SetActive(false);
            }
        }
    }
}
