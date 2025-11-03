using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class StartPad : MonoBehaviour
{
    public Statue startStatue;
    public bool requireExitBeforeRetrigger = true;

    private bool _armed = true;
    private bool _puzzleRunning = false;

    private void Reset()
    {
        var c = GetComponent<Collider2D>();
        if (c) c.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_armed) return;
        if (!other.CompareTag("Player")) return;
        if (startStatue == null) return;

        _puzzleRunning = true;
        LaserManager.Instance?.StartChainFrom(startStatue);

        if (requireExitBeforeRetrigger)
            _armed = false;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (_puzzleRunning)
        {
            _puzzleRunning = false;
            LaserManager.Instance?.HardResetAll(); // Turn off puzzle completely
        }

        if (requireExitBeforeRetrigger)
            _armed = true;
    }
}
