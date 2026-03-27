using UnityEngine;

/// <summary>
/// ChaseZone: Phát hiện khi Player vào vùng và báo cho EnemyAI để đuổi theo.
/// </summary>
public class ChaseZone : MonoBehaviour
{
    private EnemyAI enemyAI;

    private void Awake()
    {
        enemyAI = GetComponentInParent<EnemyAI>();
        if (enemyAI == null)
        {
            Debug.LogWarning($"ChaseZone on {gameObject.name} không tìm thấy EnemyAI component trong parent!");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && enemyAI != null)
        {
            enemyAI.SetInChaseZone(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && enemyAI != null)
        {
            enemyAI.SetInChaseZone(false);
        }
    }
}


