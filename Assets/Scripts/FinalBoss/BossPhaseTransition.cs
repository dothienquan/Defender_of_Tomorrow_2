using System.Collections;
using UnityEngine;

public class BossPhaseTransition : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private DialogueObject phase1EndDialogue;
    [SerializeField] private string bossName = "???";
    [SerializeField] private Sprite bossAvatar; // ✅ add avatar giống BossPhase0Sequence :contentReference[oaicite:2]{index=2}

    [Header("Phase 2")]
    [SerializeField] private GameObject phase2Prefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private GameObject phase2SpawnVfx;
    [SerializeField] private float spawnDelay = 0.5f;

    public void StartPhaseTransition()
    {
        StartCoroutine(PhaseTransitionRoutine());
    }

    private IEnumerator PhaseTransitionRoutine()
    {
        if (dialogueUI == null)
        {
            Debug.LogError($"[{name}] dialogueUI is NULL - cannot show dialogue.", this);
            yield break;
        }

        // 1) Show dialogue (✅ có avatar)
        if (phase1EndDialogue != null)
        {
            dialogueUI.Show(phase1EndDialogue, bossName, bossAvatar);
        }
        else
        {
            Debug.LogWarning($"[{name}] phase1EndDialogue is NULL.", this);
        }

        yield return null;

        // 2) Wait dialogue close
        yield return new WaitUntil(() => dialogueUI.gameObject.activeSelf == false);

        // 3) Spawn VFX
        Vector3 pos = spawnPoint ? spawnPoint.position : transform.position;

        GameObject vfxInstance = null;
        if (phase2SpawnVfx != null)
            vfxInstance = Instantiate(phase2SpawnVfx, pos, Quaternion.identity);

        // 4) Delay for VFX
        if (spawnDelay > 0f)
            yield return new WaitForSeconds(spawnDelay);

        // 5) Spawn Phase 2
        if (phase2Prefab != null)
            Instantiate(phase2Prefab, pos, Quaternion.identity);
        else
            Debug.LogError($"[{name}] phase2Prefab is NULL - cannot spawn phase 2.", this);

        // 6) Destroy VFX
        if (vfxInstance != null)
            Destroy(vfxInstance);
    }
}
