using UnityEngine;
using UnityEngine.Audio; // Cần thêm thư viện này để dùng AudioMixer

public class Sword : MonoBehaviour, IWeapon
{
    public enum ProjectilePattern { SingleToMouse, Cross4Aligned }

    [Header("Projectile Pattern")]
    [SerializeField] private ProjectilePattern projectilePattern = ProjectilePattern.SingleToMouse;

    [SerializeField] private GameObject slashAnimPrefab;
    [SerializeField] private Transform slashAnimSpawnPoint;
    [SerializeField] private float swordAttackCD = .5f;
    [SerializeField] private WeaponInfo weaponInfo;

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private float projectileLife = 0.6f;
    [SerializeField] private int projectileDamage = 10;
    [SerializeField] private LayerMask damageLayers;

    [Header("Audio Settings")]
    [Tooltip("File âm thanh khi chém")]
    [SerializeField] private AudioClip attackSound;

    [Tooltip("Gán Audio Mixer Group vào đây (ví dụ: SFX Group)")]
    [SerializeField] private AudioMixerGroup sfxMixerGroup; // --- NEW ---
    
    [Tooltip("Âm lượng (0 đến 1)")]
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 1f;

    [Tooltip("Thời gian tối thiểu giữa 2 lần phát âm thanh")]
    [SerializeField] private float minSoundInterval = 0.1f; 

    [SerializeField] private bool randomizePitch = true;

    private AudioSource audioSource;
    private float lastSoundTime = -10f; 

    private Transform weaponCollider;
    private Animator myAnimator;
    private GameObject slashAnim;

    private void Awake()
    {
        myAnimator = GetComponent<Animator>();
        
        // Setup AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // --- NEW: Gán Mixer Group cho AudioSource ---
        if (sfxMixerGroup != null)
        {
            audioSource.outputAudioMixerGroup = sfxMixerGroup;
        }
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

        slashAnim = Instantiate(slashAnimPrefab, slashAnimSpawnPoint.position, Quaternion.identity);
        slashAnim.transform.parent = this.transform.parent;

        SpawnProjectile();
        PlayAttackSound();
    }

    private void PlayAttackSound()
    {
        if (attackSound == null || audioSource == null) return;

        if (Time.time - lastSoundTime >= minSoundInterval)
        {
            if (randomizePitch)
                audioSource.pitch = Random.Range(0.9f, 1.1f);
            else
                audioSource.pitch = 1f;

            audioSource.PlayOneShot(attackSound, soundVolume);
            lastSoundTime = Time.time;
        }
    }

    private void SpawnProjectile()
    {
        if (projectilePrefab == null) return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        Vector2 dir = (mouseWorld - slashAnimSpawnPoint.position).normalized;

        switch (projectilePattern)
        {
            case ProjectilePattern.SingleToMouse:
                SpawnOneProjectile(slashAnimSpawnPoint.position, dir);
                break;
            case ProjectilePattern.Cross4Aligned:
                Vector2 perp = new Vector2(-dir.y, dir.x);
                Vector2[] dirs = { dir, -dir, perp, -perp };
                foreach (var d in dirs) SpawnOneProjectile(slashAnimSpawnPoint.position, d);
                break;
        }
    }

    private void SpawnOneProjectile(Vector3 pos, Vector2 dir)
    {
        var go = Instantiate(projectilePrefab, pos, Quaternion.identity);
        go.transform.right = dir;

        var rb = go.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = dir * projectileSpeed;

        var sp = go.GetComponent<SwordProjectile>();
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
        if (PlayerController.Instance.FacingLeft) slashAnim.GetComponent<SpriteRenderer>().flipX = true;
    }

    public void SwingDownFlipAnimEvent()
    {
        slashAnim.gameObject.transform.rotation = Quaternion.Euler(0, 0, 0);
        if (PlayerController.Instance.FacingLeft) slashAnim.GetComponent<SpriteRenderer>().flipX = true;
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