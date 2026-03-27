using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUpSpawner : MonoBehaviour
{
    [SerializeField] private GameObject goldCoin, healthGlobe, staminaGlobe;

    public void DropItems()
    {
        // Health globe always drops (100%)
        if (healthGlobe != null)
        {
            Instantiate(healthGlobe, transform.position, Quaternion.identity);
        }
        
        // Gold coin always drops (100%)
        if (goldCoin != null)
        {
            Instantiate(goldCoin, transform.position, Quaternion.identity);
        }
    }
}
