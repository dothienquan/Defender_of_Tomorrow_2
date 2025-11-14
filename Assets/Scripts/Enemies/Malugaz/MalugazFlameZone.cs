
using System.Collections;
using UnityEngine;

/// <summary>
/// Vùng lửa:
/// - Ban đầu hiện vòng tròn đỏ (indicator) trong telegraphTime giây.
/// - Sau đó bật lửa lên, gây dmg định kỳ cho Player.
/// - Tự huỷ sau lifeTime giây kể từ khi bắt đầu hiện lửa.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MalugazFlameZone : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private GameObject indicatorObject;   // vòng tròn đỏ
    [SerializeField] private GameObject flameObject;       // sprite / animation ngọn lửa

    [Header("Timing")]
    [SerializeField] private float telegraphTime = 0.75f;
    [SerializeField] private float lifeTime = 30f;

    [Header("Damage")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float damageInterval = 0.5f;

    private bool isActiveFlame = false;
    private float lastDamageTime = -999f;

    private void Start()
    {
        // cấu hình collider
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;

        if (indicatorObject != null) indicatorObject.SetActive(true);
        if (flameObject != null) flameObject.SetActive(false);

        StartCoroutine(FlameRoutine());
    }

    public void SetTelegraphTime(float time)
    {
        telegraphTime = Mathf.Max(0f, time);
    }

    private IEnumerator FlameRoutine()
    {
        if (telegraphTime > 0f)
            yield return new WaitForSeconds(telegraphTime);

        // bật lửa
        if (indicatorObject != null) indicatorObject.SetActive(false);
        if (flameObject != null) flameObject.SetActive(true);
        isActiveFlame = true;

        yield return new WaitForSeconds(lifeTime);

        Destroy(gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!isActiveFlame) return;
        if (!other.CompareTag("Player")) return;

        if (Time.time >= lastDamageTime + damageInterval)
        {
            lastDamageTime = Time.time;
            PlayerHealth.Instance.TakeDamage(damage, transform);
        }
    }
}
