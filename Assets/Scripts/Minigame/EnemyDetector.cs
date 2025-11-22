using System.Collections.Generic;
using UnityEngine;

public class EnemyDetector : MonoBehaviour
{
    public GameObject doorObject;

    private int enemyCount = 0;
    private bool playerInside = false;

    private List<GameObject> enemiesInRoom = new List<GameObject>();
    private Transform player;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            enemyCount++;
            enemiesInRoom.Add(other.gameObject);

            // If player already inside, new enemy should start chasing immediately
            if (playerInside)
                StartChase(other.gameObject);
        }

        if (other.CompareTag("Player"))
        {
            playerInside = true;
            player = other.transform;
            ActivateAllChase();
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
        }
    }

    private void Update()
    {
        if (doorObject != null)
            doorObject.SetActive(enemyCount > 0);
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
}
