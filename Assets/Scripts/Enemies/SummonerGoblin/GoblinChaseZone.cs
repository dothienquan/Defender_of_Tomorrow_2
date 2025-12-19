using UnityEngine;

/// <summary>
/// GoblinChaseZone: Phát hiện khi Player vào vùng và báo cho GoblinAI hoặc EnemyAI để đuổi theo.
/// Hỗ trợ cả GoblinAI (có SetInChaseZone) và EnemyAI (có SetInChaseZone).
/// </summary>
public class GoblinChaseZone : MonoBehaviour
{
    private GoblinAI goblinAI;
    private EnemyAI enemyAI;

    private void Awake()
    {
        goblinAI = GetComponentInParent<GoblinAI>();
        enemyAI = GetComponentInParent<EnemyAI>();
        
        if (goblinAI == null && enemyAI == null)
        {
            Debug.LogWarning($"GoblinChaseZone on {gameObject.name} không tìm thấy GoblinAI hoặc EnemyAI component trong parent!");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (goblinAI != null)
            {
                goblinAI.SetInChaseZone(true);
            }
            else if (enemyAI != null)
            {
                enemyAI.SetInChaseZone(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (goblinAI != null)
            {
                goblinAI.SetInChaseZone(false);
            }
            else if (enemyAI != null)
            {
                enemyAI.SetInChaseZone(false);
            }
        }
    }
}
