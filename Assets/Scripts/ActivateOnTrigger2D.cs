using UnityEngine;
using DG.Tweening;

public class ActivateOnTrigger2D : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private GameObject targetObject;

    [Header("VFX")]
    [SerializeField] private ParticleSystem vfxPrefab;

    [Header("Appear Effect")]
    [SerializeField] private float appearDuration = 0.25f;
    [SerializeField] private Ease appearEase = Ease.OutBack;

    private bool activated;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (activated) return;
        if (!other.CompareTag("Player")) return;

        activated = true;

        // Spawn VFX
        if (vfxPrefab != null)
        {
            Instantiate(
                vfxPrefab,
                targetObject.transform.position,
                Quaternion.identity
            );
        }

        // Appear effect
        targetObject.transform.localScale = Vector3.zero;
        targetObject.SetActive(true);

        targetObject.transform
            .DOScale(Vector3.one, appearDuration)
            .SetEase(appearEase);
    }
}
