
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bow : MonoBehaviour, IWeapon
{
    [Header("Base")]
    [SerializeField] private WeaponInfo weaponInfo;
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private Transform arrowSpawnPoint;

    [Header("Multishot")]
    [Tooltip("Số lượng mũi tên mỗi lần bắn")]
    [SerializeField] private int projectilesPerShot = 5;
    [Tooltip("Độ mở góc (độ) của loạt bắn. 0 = bắn thẳng một hàng.")]
    [SerializeField] private float spreadAngle = 20f;

    [Header("Homing Feel")]
    [Tooltip("Độ trễ homing giữa các mũi (giây) để tránh chụm")]
    [SerializeField] private float perProjectileHomingDelayStep = 0.05f;

    readonly int FIRE_HASH = Animator.StringToHash("Fire");
    private Animator myAnimator;

    private void Awake()
    {
        myAnimator = GetComponent<Animator>();
    }

    public void Attack()
    {
        myAnimator.SetTrigger(FIRE_HASH);

        // Lấy điểm chuột (target) trên world
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        // Hướng cơ sở từ point bắn tới chuột
        Vector2 toMouse = (mouseWorld - arrowSpawnPoint.position).normalized;
        float baseAngle = Mathf.Atan2(toMouse.y, toMouse.x) * Mathf.Rad2Deg;

        int count = Mathf.Max(1, projectilesPerShot);
        float totalSpread = (count > 1) ? spreadAngle : 0f;
        float startAngle = baseAngle - totalSpread * 0.5f;
        float step = (count > 1) ? (totalSpread / (count - 1)) : 0f;

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + step * i;
            Quaternion rot = Quaternion.AngleAxis(angle, Vector3.forward);

            GameObject newArrow = Instantiate(arrowPrefab, arrowSpawnPoint.position, rot);

            Projectile proj = newArrow.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.UpdateProjectileRange(weaponInfo.weaponRange);
                proj.SetTarget(mouseWorld);

                // Stagger homing: mỗi mũi chờ thêm một chút trước khi bám mục tiêu
                float delay = perProjectileHomingDelayStep * i;
                proj.SetHomingDelay(delay);
            }
        }
    }

    public WeaponInfo GetWeaponInfo()
    {
        return weaponInfo;
    }
}
