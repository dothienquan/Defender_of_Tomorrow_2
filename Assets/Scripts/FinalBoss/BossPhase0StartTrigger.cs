using UnityEngine;

/// <summary>
/// Gắn lên 1 Trigger collider để bắt đầu Phase0.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BossPhase0StartTrigger : MonoBehaviour
{
    [SerializeField] private BossPhase0Controller phase0Controller;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool oneShot = true;

    private bool _used;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_used && oneShot) return;
        if (!other.CompareTag(playerTag)) return;

        _used = true;
        if (phase0Controller != null)
            phase0Controller.StartPhase0();

        if (oneShot)
            gameObject.SetActive(false);
    }
}
