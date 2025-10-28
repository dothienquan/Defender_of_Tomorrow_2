using UnityEngine;
using System.Linq;

public class MiniGameManager : MonoBehaviour
{
    [Header("References (optional)")]
    public TowerMinigame[] towers;
    public Color completedColor = Color.green;

    private void Awake()
    {
        if (towers == null || towers.Length == 0)
            towers = FindObjectsOfType<TowerMinigame>(true);
    }

    private void OnEnable()
    {
        foreach (var t in towers)
            t.OnTowerCompleted += HandleTowerCompleted;
    }

    private void OnDisable()
    {
        foreach (var t in towers)
            t.OnTowerCompleted -= HandleTowerCompleted;
    }

    private void HandleTowerCompleted(TowerMinigame t)
    {
        bool allDone = towers.All(x => x != null && x.IsCompleted);
        if (allDone)
        {
            foreach (var tower in towers)
                tower.SetIndicator(completedColor);
            Debug.Log("[GameManager] All towers completed.");
        }
    }
}