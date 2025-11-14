using UnityEngine;

public class EnemyDetector : MonoBehaviour
{
    public GameObject doorObject;  
    private int enemyCount = 0;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
            enemyCount++;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
            enemyCount--;
    }

    private void Update()
    {
        if (doorObject != null)
            doorObject.SetActive(enemyCount > 0);
    }
}
