using UnityEngine;
using System.Collections;

public class ExplodeOnTrigger : MonoBehaviour
{
    [SerializeField] private GameObject explodeVfxPrefab;
    [SerializeField] private float destroyDelay = 0.3f;

    private bool triggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;

        Instantiate(explodeVfxPrefab, transform.position, Quaternion.identity);
        StartCoroutine(DestroyAfterDelay());
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }
}
