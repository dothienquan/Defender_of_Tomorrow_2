using UnityEngine;

[System.Serializable]
public class Wave
{
    public GameObject enemyPrefab;  // Kéo prefab quái sẵn có vào đây
    public int count = 5;
    public float spawnInterval = 0.4f;
}