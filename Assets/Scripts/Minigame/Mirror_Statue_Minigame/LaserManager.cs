using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserManager : MonoBehaviour
{
    public static LaserManager Instance { get; private set; }

    [Header("Chain Settings")]
    [Tooltip("Delay between a statue getting hit and the next one firing.")]
    public float hopDelay = 0.2f;

    [Tooltip("Optional: stop after N steps to avoid infinite loops if you accidentally make a cycle.")]
    public int safetyMaxSteps = 32;

    private readonly List<Statue> _statues = new List<Statue>();

    // version ticks up whenever we reset/restart; running coroutines bail if version changes
    private int _version = 0;

    // Remember the last starting statue so rotations can rebuild from it.
    private Statue _lastStart;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Register(Statue s)
    {
        if (!_statues.Contains(s)) _statues.Add(s);
    }

    public void Unregister(Statue s)
    {
        _statues.Remove(s);
    }

    public void HardResetAll()
    {
        _version++;
        foreach (var s in _statues)
        {
            s.DeactivateInstant();
        }
    }

    public void StartChainFrom(Statue start)
    {
        _lastStart = start;
        _version++;
        foreach (var s in _statues) s.DeactivateInstant();
        StartCoroutine(RunChainSequential(start, _version));
    }

    public void RebuildFromLastStart()
    {
        if (_lastStart == null) return;
        StartChainFrom(_lastStart);
    }

    private IEnumerator RunChainSequential(Statue start, int version)
    {
        var visited = new HashSet<Statue>();
        var current = start;
        int steps = 0;

        while (current != null && steps < safetyMaxSteps)
        {
            if (version != _version) yield break; // cancelled by a reset

            visited.Add(current);

            // Activate current, fire laser, get next statue if any.
            Statue next = current.ActivateAndCast(out Vector3 hitPoint);
            if (version != _version) yield break;

            // (Optional) you could place a small visual tick here

            if (next == null || visited.Contains(next))
            {
                // Hit nothing or we would loop; stop chain here.
                yield break;
            }

            // Wait before the next statue activates (sequential style)
            yield return new WaitForSeconds(hopDelay);

            current = next;
            steps++;
        }
    }

    // Call when a statue has been rotated
    public void OnStatueRotated()
    {
        HardResetAll();
        // small grace delay so the off state is visible, then rebuild
        StartCoroutine(RestartAfterDelay(0.15f));
    }

    private IEnumerator RestartAfterDelay(float t)
    {
        int startedVersion = ++_version;
        yield return new WaitForSeconds(t);
        if (startedVersion != _version) yield break;
        RebuildFromLastStart();
    }
}
