using UnityEngine;

public class Sword : MonoBehaviour, IWeapon
{
    public enum ProjectilePattern { SingleToMouse, Cross4Aligned }

    [Header("Projectile Pattern")]
    [SerializeField] private ProjectilePattern projectilePattern = ProjectilePattern.SingleToMouse;

    [SerializeField] private GameObject slashAnimPrefab;
    [SerializeField] private Transform slashAnimSpawnPoint;
    [SerializeField] private float swordAttackCD = .5f;
    [SerializeField] private WeaponInfo weaponInfo;

    // NEW: cấu hình projectile
    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;   // kéo prefab vào
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private float projectileLife = 0.6f;
    [SerializeField] private int projectileDamage = 10;     // hoặc lấy từ weaponInfo nếu bạn có
    [SerializeField] private LayerMask damageLayers;        // layer của Enemy

    private Transform weaponCollider;
    private Animator myAnimator;
    private GameObject slashAnim;

    private void Awake()
    {
        myAnimator = GetComponent<Animator>();
    }

    private void Start()
    {
        weaponCollider = PlayerController.Instance.GetWeaponCollider();
        if (slashAnimSpawnPoint == null)
            slashAnimSpawnPoint = GameObject.Find("SlashSpawnPoint").transform;
    }

    private void Update()
    {
        MouseFollowWithOffset();
    }

    public WeaponInfo GetWeaponInfo()
    {
        return weaponInfo;
    }

    public void Attack()
    {
        myAnimator.SetTrigger("Attack");
        weaponCollider.gameObject.SetActive(true);

        // slash vệt chém như cũ
        slashAnim = Instantiate(slashAnimPrefab, slashAnimSpawnPoint.position, Quaternion.identity);
        slashAnim.transform.parent = this.transform.parent;

        // NEW: spawn projectile gây damage
        SpawnProjectile();
    }

    private void SpawnProjectile()
    {
        if (projectilePrefab == null) return;

        // hướng tấn công theo chuột (viên chính)
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        Vector2 dir = (mouseWorld - slashAnimSpawnPoint.position).normalized;

        switch (projectilePattern)
        {
            case ProjectilePattern.SingleToMouse:
                {
                    // 1 viên theo hướng tấn công
                    SpawnOneProjectile(slashAnimSpawnPoint.position, dir);
                    break;
                }
            case ProjectilePattern.Cross4Aligned:
                {
                    // 4 viên tạo dấu + xoay theo hướng tấn công
                    Vector2 perp = new Vector2(-dir.y, dir.x);
                    Vector2[] dirs = { dir, -dir, perp, -perp };
                    foreach (var d in dirs)
                        SpawnOneProjectile(slashAnimSpawnPoint.position, d);
                    break;
                }
        }
    }

    private void SpawnOneProjectile(Vector3 pos, Vector2 dir)
    {
        var go = Instantiate(projectilePrefab, pos, Quaternion.identity);

        // xoay sprite theo hướng bay (nếu prefab cần)
        go.transform.right = dir;

        var rb = go.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = dir * projectileSpeed;

        var sp = go.GetComponent<SwordProjectile>(); // nếu bạn dùng script này cho projectile
        if (sp != null)
        {
            sp.damage = projectileDamage;
            sp.targetLayers = damageLayers;
        }

        Destroy(go, projectileLife);
    }
    public void DoneAttackingAnimEvent()
    {
        weaponCollider.gameObject.SetActive(false);
    }

    public void SwingUpFlipAnimEvent()
    {
        slashAnim.gameObject.transform.rotation = Quaternion.Euler(-180, 0, 0);
        if (PlayerController.Instance.FacingLeft)
        {
            slashAnim.GetComponent<SpriteRenderer>().flipX = true;
        }
    }

    public void SwingDownFlipAnimEvent()
    {
        slashAnim.gameObject.transform.rotation = Quaternion.Euler(0, 0, 0);
        if (PlayerController.Instance.FacingLeft)
        {
            slashAnim.GetComponent<SpriteRenderer>().flipX = true;
        }
    }

    private void MouseFollowWithOffset()
    {
        Vector3 mousePos = Input.mousePosition;
        Vector3 playerScreenPoint = Camera.main.WorldToScreenPoint(PlayerController.Instance.transform.position);
        float angle = Mathf.Atan2(mousePos.y, mousePos.x) * Mathf.Rad2Deg;

        if (mousePos.x < playerScreenPoint.x)
        {
            ActiveWeapon.Instance.transform.rotation = Quaternion.Euler(0, -180, angle);
            weaponCollider.transform.rotation = Quaternion.Euler(0, -180, 0);
        }
        else
        {
            ActiveWeapon.Instance.transform.rotation = Quaternion.Euler(0, 0, angle);
            weaponCollider.transform.rotation = Quaternion.Euler(0, 0, 0);
        }
    }
}
