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

        // 2. Chờ dialogue mở xong
        yield return null;

        // 3. Chờ dialogue đóng
        yield return new WaitUntil(() => dialogueUI.gameObject.activeSelf == false);

        // 4. Spawn Phase 2
        Vector3 pos = spawnPoint ? spawnPoint.position : transform.position;

        if (phase2SpawnVfx != null)
            Instantiate(phase2SpawnVfx, pos, Quaternion.identity);

        if (spawnDelay > 0f)
            yield return new WaitForSeconds(spawnDelay);

        Instantiate(phase2Prefab, pos, Quaternion.identity);
    }
}
