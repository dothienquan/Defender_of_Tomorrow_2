using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyWave
{
    public GameObject enemyPrefab;
    public int count = 3;
    public float spawnInterval = 0.4f;
}

public class EnemyDetector : MonoBehaviour
{
    [Header("Door")]
    public GameObject doorObject;

    [Header("Wave Spawn")]
    public EnemyWave[] waves = new EnemyWave[2];
    public Transform[] spawnPoints;
    public float spawnRadius = 4f;

    private int enemyCount = 0;
    private bool playerInside = false;
    private bool wavesStarted = false;
    private int currentWaveIndex = -1;
    private int aliveInCurrentWave = 0;
    
    // Biến mới: Đánh dấu đã dọn dẹp xong
    private bool isCleared = false;

    private List<GameObject> enemiesInRoom = new List<GameObject>();
    private Transform player;
    private Coroutine spawnCoroutine;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (!enemiesInRoom.Contains(other.gameObject))
            {
                enemyCount++;
                enemiesInRoom.Add(other.gameObject);

                if (playerInside)
                    StartChase(other.gameObject);
            }
        }

        if (other.CompareTag("Player"))
        {
            playerInside = true;
            player = other.transform;
            ActivateAllChase();
            
            // CHỈNH SỬA: Kiểm tra thêm !isCleared
            if (!isCleared && !wavesStarted && waves != null && waves.Length > 0)
            {
                wavesStarted = true;
                StartWaveSpawning();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            enemyCount--;
            enemiesInRoom.Remove(other.gameObject);
            StopChase(other.gameObject);
        }

        if (other.CompareTag("Player"))
        {
            playerInside = false;
            player = null;
            StopAllChase();
            
            // CHỈNH SỬA: Chỉ reset wave nếu chưa dọn dẹp xong
            if (!isCleared)
            {
                if (spawnCoroutine != null)
                {
                    StopCoroutine(spawnCoroutine);
                    spawnCoroutine = null;
                }
                wavesStarted = false;
                currentWaveIndex = -1;
                
                // Mở cửa tạm thời nếu người chơi bỏ chạy (reset phòng)
                if (doorObject != null) doorObject.SetActive(false);
            }
        }
    }

    private void Update()
    {
        if (doorObject != null)
        {
            // Cửa đóng khi có enemy hoặc đang chạy wave (và chưa clear)
            bool hasEnemies = enemyCount > 0 || (wavesStarted && currentWaveIndex < waves.Length - 1);
            
            // Nếu đã clear thì cửa luôn mở (SetActive false)
            if (isCleared) 
                doorObject.SetActive(false);
            else
                doorObject.SetActive(hasEnemies);
        }
    }

    private void ActivateAllChase()
    {
        foreach (var enemy in enemiesInRoom)
            StartChase(enemy);
    }

    private void StopAllChase()
    {
        foreach (var enemy in enemiesInRoom)
            StopChase(enemy);
    }

    private void StartChase(GameObject enemy)
    {
        var chase = enemy.GetComponent<EnemyChase>();
        if (chase != null)
            chase.BeginChase(player);
    }

    private void StopChase(GameObject enemy)
    {
        var chase = enemy.GetComponent<EnemyChase>();
        if (chase != null)
            chase.StopChase();
    }

    private void StartWaveSpawning()
    {
        currentWaveIndex = -1;
        NextWave();
    }

    private void NextWave()
    {
        currentWaveIndex++;
        
        // Tất cả wave đã hoàn thành
        if (currentWaveIndex >= waves.Length)
        {
            // CHỈNH SỬA: Đánh dấu hoàn thành
            isCleared = true;
            wavesStarted = false;

            if (doorObject != null)
                doorObject.SetActive(false);
            return;
        }

        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);
        
        spawnCoroutine = StartCoroutine(SpawnWaveCoroutine(waves[currentWaveIndex]));
    }

    private IEnumerator SpawnWaveCoroutine(EnemyWave wave)
    {
        if (wave == null || wave.enemyPrefab == null)
        {
            NextWave();
            yield break;
        }

        aliveInCurrentWave = 0;
        int spawned = 0;

        while (spawned < wave.count)
        {
            Vector3 spawnPos = GetSpawnPosition();
            GameObject enemy = Instantiate(wave.enemyPrefab, spawnPos, Quaternion.identity);
            
            enemiesInRoom.Add(enemy);
            enemyCount++;
            aliveInCurrentWave++;

            SetupEnemyDeathReporting(enemy);

            if (playerInside)
                StartChase(enemy);

            spawned++;
            yield return new WaitForSeconds(wave.spawnInterval);
        }
    }

    private void SetupEnemyDeathReporting(GameObject enemy)
    {
        EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.OnDeath += () => OnEnemyDeath(enemy);
        }
        else
        {
            var reporter = enemy.GetComponent<EnemyDeathReporter>();
            if (reporter == null)
                reporter = enemy.AddComponent<EnemyDeathReporter>();
            reporter.detector = this;
            reporter.enemyGameObject = enemy;
        }
    }

    public void OnEnemyDeath(GameObject enemy)
    {
        if (enemy == null) return;

        if (enemiesInRoom.Contains(enemy))
        {
            enemyCount--;
            enemiesInRoom.Remove(enemy);

            if (wavesStarted && currentWaveIndex >= 0 && currentWaveIndex < waves.Length)
            {
                aliveInCurrentWave = Mathf.Max(0, aliveInCurrentWave - 1);
                
                if (aliveInCurrentWave == 0)
                {
                    NextWave();
                }
            }
        }
    }

    private Vector3 GetSpawnPosition()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            return spawnPoint.position;
        }

        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * spawnRadius;
        return transform.position + offset;
    }
}

// Giữ nguyên class phụ trợ
public class EnemyDeathReporter : MonoBehaviour
{
    public EnemyDetector detector;
    public GameObject enemyGameObject;

    private void OnDestroy()
    {
        if (detector != null && enemyGameObject != null)
        {
            detector.OnEnemyDeath(enemyGameObject);
        }
    }
}