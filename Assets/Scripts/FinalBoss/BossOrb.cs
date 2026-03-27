using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class BossOrb : MonoBehaviour
{
    [Header("Orbit")]
    [SerializeField] private float orbitRadius = 2.5f;
    [SerializeField] private float orbitSpeed = 40f;

    [Header("Shooting")]
    [SerializeField] private GameObject pentagonProjectilePrefab;
    [SerializeField] private float pentagonShootInterval = 1.2f;

    [Header("Ground Return")]
    [SerializeField] private float returnToOrbitTime = 6f; // ✅ SAU 6S TỰ QUAY LẠI

    private IBossOrbOwner owner;
    private Transform center;
    private Transform player;

    private float angle;
    private bool isGrounded = false;

    private Coroutine turretRoutine;
    private Coroutine returnRoutine;

    private EnemyHealth enemyHealth;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        enemyHealth.SetDestroyOnDeath(false);
        enemyHealth.OnDeath += OnOrbDeath;
    }

    public void Setup(
        IBossOrbOwner owner,
        Transform center,
        Transform player,
        float startAngleDeg,
        float radiusOverride = -1f)
    {
        this.owner = owner;
        this.center = center;
        this.player = player;

        if (radiusOverride > 0f)
            orbitRadius = radiusOverride;

        isGrounded = false;
        enemyHealth.ResetHealthToMax();

        angle = startAngleDeg;
        UpdateOrbitPosition();

        // ✅ Orb bay cũng bắn luôn
        if (turretRoutine != null)
            StopCoroutine(turretRoutine);

        turretRoutine = StartCoroutine(PentagonShootRoutine());
    }

    private void Update()
    {
        if (center == null || isGrounded) return;

        angle += orbitSpeed * Time.deltaTime;
        UpdateOrbitPosition();
    }

    private void UpdateOrbitPosition()
    {
        if (center == null) return;

        float rad = angle * Mathf.Deg2Rad;
        Vector2 offset = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * orbitRadius;
        transform.position = (Vector2)center.position + offset;
    }

    private void OnOrbDeath()
    {
        Ground();
    }

    private void Ground()
    {
        if (isGrounded) return;

        isGrounded = true;

        // rơi xuống nhẹ cho dễ thấy
        transform.position += Vector3.down * 1f;

        // ✅ vẫn báo boss nếu bạn còn dùng logic này
        owner?.NotifyOrbGrounded(this);

        // ✅ Sau 6s tự quay lại bay
        if (returnRoutine != null)
            StopCoroutine(returnRoutine);

        returnRoutine = StartCoroutine(ReturnToOrbitAfterDelay());
    }

    private IEnumerator ReturnToOrbitAfterDelay()
    {
        yield return new WaitForSeconds(returnToOrbitTime);

        // ✅ Tự hồi lại không cần chờ orb khác
        ResetToOrbit(angle);
    }

    public void ResetToOrbit(float startAngleDeg)
    {
        isGrounded = false;

        enemyHealth.ResetHealthToMax();

        angle = startAngleDeg;
        UpdateOrbitPosition();

        if (turretRoutine == null)
            turretRoutine = StartCoroutine(PentagonShootRoutine());
    }

    private IEnumerator PentagonShootRoutine()
    {
        while (true)
        {
            int count = 5;
            float step = 360f / count;
            float start = Random.Range(0f, 360f);

            for (int i = 0; i < count; i++)
            {
                float ang = (start + step * i) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));

                if (pentagonProjectilePrefab != null)
                {
                    GameObject go = Instantiate(
                        pentagonProjectilePrefab,
                        transform.position,
                        Quaternion.identity
                    );

                    var proj = go.GetComponent<LightningProjectile>();
                    if (proj != null)
                        proj.Init(dir);
                }
            }

            yield return new WaitForSeconds(pentagonShootInterval);
        }
    }

    private void OnDestroy()
    {
        if (enemyHealth != null)
            enemyHealth.OnDeath -= OnOrbDeath;
    }
}
