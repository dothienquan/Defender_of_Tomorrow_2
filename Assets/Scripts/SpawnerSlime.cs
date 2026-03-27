using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnerSlime : MonoBehaviour
{
    [SerializeField] private GameObject slime;
    [SerializeField] private int quantity = 3;
    public void SpawnerSlimes()
    {
        for (int i = 0; i < quantity; i++)
        {
            Instantiate(slime, transform.position, Quaternion.identity);
        }
        Debug.Log("sinh slime");
    }
    
}
