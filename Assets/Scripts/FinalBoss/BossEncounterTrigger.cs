using UnityEngine;

/// <summary>
/// Simple trigger that starts the boss encounter once the player enters.
/// Attach to a trigger collider area (IsTrigger = true).
/// </summary>
public class BossEncounterTrigger : MonoBehaviour
{
    [SerializeField] private BossIntroSummonController bossController;
    [SerializeField] private bool disableAfterTriggered = true;

    private bool triggered = false;

    private void Reset()
    {
        // Try auto find in scene
        if (bossController == null)
            bossController = FindFirstObjectByType<BossIntroSummonController>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;
        if (bossController != null)
            bossController.StartEncounter();

        if (disableAfterTriggered)
            gameObject.SetActive(false);
    }
}