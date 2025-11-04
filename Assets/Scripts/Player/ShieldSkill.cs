using UnityEngine;

/// <summary>
/// ShieldSkillNoLayerIgnore
/// - Nhấn C để kích hoạt skill prefab (child của Player)
/// - Tiêu 1 stamina
/// - KHÔNG dùng IgnoreLayerCollision (toàn cục). Prefab tự chặn bằng IgnoreCollision THEO CẶP collider.
/// </summary>
public class ShieldSkill : MonoBehaviour
{
    [Header("Kích hoạt")]
    public KeyCode key = KeyCode.C;

    [Header("Tiêu hao & Hồi chiêu")]
    public int staminaCost = 1;
    public float cooldown = 0.5f;

    [Header("Prefab kỹ năng")]
    public ShieldInstance shieldPrefab;
    public Transform attachPoint;

    private float _nextAllowedTime;

    private void Awake()
    {
        if (attachPoint == null) attachPoint = transform;
    }

    private void Update()
    {
        if (Input.GetKeyDown(key))
        {
            TryCast();
        }
    }

    public void TryCast()
    {
        if (Time.time < _nextAllowedTime) return;
        if (Stamina.Instance == null) return;
        if (Stamina.Instance.CurrentStamina < staminaCost) return;
        if (shieldPrefab == null) return;

        for (int i = 0; i < staminaCost; i++)
            Stamina.Instance.UseStamina();

        var inst = Instantiate(shieldPrefab, attachPoint.position, Quaternion.identity, attachPoint);
        inst.owner = gameObject;

        _nextAllowedTime = Time.time + cooldown;
    }
}