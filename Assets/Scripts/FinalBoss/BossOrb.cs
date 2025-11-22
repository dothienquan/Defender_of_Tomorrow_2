using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class BossOrb : MonoBehaviour
{
    [Header("Orbit")]
    [SerializeField] private float orbitRadius = 2.5f;
    [SerializeField] private float orbitSpeed = 40f; // độ/giây

    [Header("Ground turret")]
    [SerializeField] private GameObject pentagonProjectilePrefab;
    [SerializeField] private float pentagonShootInterval = 1.2f;

    private BossController controller;
    private Transform center;   // boss
    private Transform player;

    private float angle;        // độ
    private bool isGrounded = false;
    private Coroutine turretRoutine;

    private EnemyHealth enemyHealth;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        enemyHealth.SetDestroyOnDeath(false);
        enemyHealth.OnDeath += OnOrbDeath;
    }

    // Boss gọi khi spawn lần đầu
    public void Setup(BossController controller, Transform center, Transform player,
                      float startAngleDeg, float radiusOverride = -1f)
    {
        this.controller = controller;
        this.center = center;
        this.player = player;

        if (radiusOverride > 0f)
            orbitRadius = radiusOverride;

        isGrounded = false;
        enemyHealth.ResetHealthToMax();

        angle = startAngleDeg;
        UpdateOrbitPosition();
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

        // rơi xuống 1 tí cho dễ thấy
        transform.position = new Vector2(transform.position.x, transform.position.y - 1f);

        controller.NotifyOrbGrounded(this);

        turretRoutine = StartCoroutine(PentagonShootRoutine());
    }

    /// <summary>
    /// Gọi khi boss hết stun: orb full máu + trở lại quỹ đạo ở góc mới,
    /// KHÔNG quay tiếp từ chỗ rơi.
    /// </summary>
    public void ResetToOrbit(float startAngleDeg)
    {
        isGrounded = false;

        if (turretRoutine != null)
        {
            StopCoroutine(turretRoutine);
            turretRoutine = null;
        }

        enemyHealth.ResetHealthToMax();

        angle = startAngleDeg;
        UpdateOrbitPosition();
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
