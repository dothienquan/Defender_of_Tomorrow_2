using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BossPhase0Sequence : MonoBehaviour
{
    [Header("Dialogue (uses DialogueUI.Show like NPCDialogue)")]
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private DialogueObject dialog1;
    [SerializeField] private DialogueObject dialog2;
    [SerializeField] private string bossName = "Boss";
    [SerializeField] private Sprite bossAvatar;

    [Header("Summon (phase 0 enemy)")]
    [SerializeField] private GameObject summonedEnemyPrefab;
    [SerializeField] private Transform summonSpawnPoint;
    [SerializeField] private GameObject summonVfxPrefab;
    [SerializeField] private float summonDelay = 0.35f;

    [Header("Phase 1 Spawn (spawn immediately after dialog 2)")]
    [Tooltip("Kéo object có SpawnOnPlayerEnter (spawner phase 1) vào đây.")]
    [SerializeField] private SpawnOnPlayerEnter phase1Spawner;

    [Header("Phase Switch (same frame)")]
    [SerializeField] private GameObject[] phase1ObjectsToActivate;
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

        // ✅ Spawn phase 1 right after dialog 2 ends
        if (phase1Spawner != null)
        {
            if (!phase1Spawner.gameObject.activeInHierarchy)
                phase1Spawner.gameObject.SetActive(true);

            phase1Spawner.SpawnNow();
        }
        else
        {
            Debug.LogWarning($"[{name}] phase1Spawner is NULL - phase 1 will not spawn automatically.", this);
        }

        // -------- Switch phase (same frame) --------
        ActivatePhase1DeactivatePhase0SameFrame();
    }

    private IEnumerator WaitDialogueClose()
    {
        int safety = 0;
        while (!dialogueUI.gameObject.activeSelf && safety < 10)
        {
            safety++;
            yield return null;
        }

        while (dialogueUI.gameObject.activeSelf)
            yield return null;
    }

    private IEnumerator WaitEnemyDeath(GameObject enemy)
    {
        if (enemy == null) yield break;

        bool died = false;

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
            while (enemy != null)
                yield return null;
        }
    }

    private void ActivatePhase1DeactivatePhase0SameFrame()
    {
        if (phase1ObjectsToActivate != null)
        {
            for (int i = 0; i < phase1ObjectsToActivate.Length; i++)
            {
                var go = phase1ObjectsToActivate[i];
                if (go != null) go.SetActive(true);
            }
        }

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
