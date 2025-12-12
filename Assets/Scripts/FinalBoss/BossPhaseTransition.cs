using System.Collections;
using UnityEngine;

public class BossPhaseTransition : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private DialogueObject phase1EndDialogue;
    [SerializeField] private string bossName = "???";

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
        // 1. Show dialogue
        dialogueUI.Show(phase1EndDialogue, bossName);

        yield return null;

        // 2. Chờ dialogue đóng
        yield return new WaitUntil(() => dialogueUI.gameObject.activeSelf == false);

        // 3. Spawn VFX
        Vector3 pos = spawnPoint ? spawnPoint.position : transform.position;

        GameObject vfxInstance = null;
        if (phase2SpawnVfx != null)
        {
            vfxInstance = Instantiate(phase2SpawnVfx, pos, Quaternion.identity);
        }

        // 4. Delay cho VFX chạy
        if (spawnDelay > 0f)
            yield return new WaitForSeconds(spawnDelay);

        // 5. Spawn Phase 2
        Instantiate(phase2Prefab, pos, Quaternion.identity);

        // 6. Destroy VFX
        if (vfxInstance != null)
            Destroy(vfxInstance);
    }

}
