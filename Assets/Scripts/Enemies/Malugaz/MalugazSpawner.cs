using System.Collections;
using System.Linq;
using UnityEngine;

public class MalugazSpawner : MonoBehaviour
{
    [Header("Boss Activation Setup")]
    [SerializeField] private GameObject malugazObject;
    [Tooltip("Object có sẵn trên map, sẽ được kích hoạt sau khi hoàn thành minigame")]

    [Header("Minigame")]
    [SerializeField] private MiniGameManager miniGameManager;

    private bool hasActivated = false;

    private void Awake()
    {
        // Đảm bảo object ban đầu tắt
        if (malugazObject != null)
        {
            malugazObject.SetActive(false);
        }
    }

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
                ActivateBoss();
                yield break;
            }

            yield return new WaitForSeconds(0.3f);
        }
    }

    private void ActivateBoss()
    {
        if (hasActivated) return;
        if (malugazObject == null)
        {
            Debug.LogWarning("[MalugazSpawner] Malugaz object is not assigned!");
            return;
        }

        hasActivated = true;
        malugazObject.SetActive(true);
        Debug.Log("Malugaz Activated!");
    }
}
