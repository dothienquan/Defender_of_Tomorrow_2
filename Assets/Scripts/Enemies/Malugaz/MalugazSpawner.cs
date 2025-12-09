using System.Collections;
using System.Linq;
using UnityEngine;

public class MalugazSpawner : MonoBehaviour
{
    [Header("Boss Spawn Setup")]
    [SerializeField] private GameObject malugazPrefab;
    [SerializeField] private Transform bossSpawnPoint;

    [Header("Minigame")]
    [SerializeField] private MiniGameManager miniGameManager;

    private bool hasSpawned = false;

    private void Start()
    {
        if (miniGameManager == null)
            miniGameManager = FindObjectOfType<MiniGameManager>();

        StartCoroutine(WaitForMiniGameComplete());
    }

    private IEnumerator WaitForMiniGameComplete()
    {
        while (true)
        {
            if (miniGameManager != null &&
                miniGameManager.towers != null &&
                miniGameManager.towers.Length > 0 &&
                miniGameManager.towers.All(t => t != null && t.IsCompleted))
            {
                SpawnBoss();
                yield break;
            }

            yield return new WaitForSeconds(0.3f);
        }
    }

    private void SpawnBoss()
    {
        if (hasSpawned) return;
        hasSpawned = true;

        GameObject boss = Instantiate(
            malugazPrefab,
            bossSpawnPoint.position,
            Quaternion.identity
        );

        Debug.Log("Malugaz Spawned!");
    }
}
