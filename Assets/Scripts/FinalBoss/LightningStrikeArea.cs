using System;
using System.Collections;
using UnityEngine;

public class LightningStrikeArea : MonoBehaviour
{
    [SerializeField] private float telegraphTime = 0.6f;
    [SerializeField] private float damageRadius = 0.8f;
    [SerializeField] private int damage = 1;

    private void Start()
    {
        StartCoroutine(StrikeRoutine());
    }

    private IEnumerator StrikeRoutine()
    {
        // TODO: bật animation telegraph ở đây (scale, màu,…)
        yield return new WaitForSeconds(telegraphTime);

        // gây damage nếu player đứng trong vùng
        if (PlayerHealth.Instance != null)
        {
            float dist = Vector2.Distance(transform.position, PlayerHealth.Instance.transform.position);
            if (dist <= damageRadius)
            {
                PlayerHealth.Instance.TakeDamage(damage, transform); 
            }
        }

        // TODO: play VFX sét đánh rồi destroy
        Destroy(gameObject, 0.1f);
    }
}
