
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// FIX: Chặn sát thương chắc chắn khi bật Shield mà không dùng IgnoreLayerCollision.
/// - Bỏ điều kiện attachedRigidbody (kẻ địch không có RB vẫn bị chặn).
/// - Lấy TẤT CẢ colliders từ enemy.transform.root để không sót hitbox con.
/// - Bỏ qua collider vũ khí của Player (có DamageSource) để Player vẫn gây damage bình thường.
/// - Áp dụng mỗi frame, theo bán kính.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ShieldInstance : MonoBehaviour
{
    [Header("Thời lượng & Bán kính")]
    public float duration = 1.0f;
    public float radius = 1.8f;

    [Header("Nhận diện Enemy")]
    public LayerMask enemyLayers;
    public string enemyTag = "";

    [Header("Projectile của Enemy")]
    public LayerMask projectileLayers;
    public string projectileTag = "";

    public bool keepAsTrigger = true;

    [HideInInspector] public GameObject owner;

    private Collider2D[] _ownerCols;
    private readonly HashSet<(Collider2D, Collider2D)> _ignoredPairs = new HashSet<(Collider2D, Collider2D)>();
    private Collider2D _selfCol;

    private void Awake()
    {
        _selfCol = GetComponent<Collider2D>();
        if (_selfCol != null) _selfCol.isTrigger = keepAsTrigger;
    }

    private void OnEnable()
    {
        CacheOwnerColliders();
        Invoke(nameof(SelfDestruct), duration);
    }

    private void Update()
    {
        if (owner == null) return;

        // Đồng bộ theo Player
        transform.position = owner.transform.position;

        // Áp dụng bỏ qua va chạm theo cặp mỗi frame
        ApplyIgnoreCollisionPairs();
    }

    private void OnDisable()
    {
        RestoreAllPairs();
    }

    private void CacheOwnerColliders()
    {
        if (owner == null)
        {
            _ownerCols = new Collider2D[0];
            return;
        }
        _ownerCols = owner.GetComponentsInChildren<Collider2D>(includeInactive: false);
    }

    private bool IsEnemy(GameObject go)
    {
        bool ok = true;
        if (enemyLayers.value != 0)
            ok &= ((1 << go.layer) & enemyLayers.value) != 0;
        if (!string.IsNullOrEmpty(enemyTag))
            ok &= go.CompareTag(enemyTag);
        return ok;
    }

    private bool IsProjectile(GameObject go)
    {
        bool ok = true;
        if (projectileLayers.value != 0)
            ok &= ((1 << go.layer) & projectileLayers.value) != 0;
        if (!string.IsNullOrEmpty(projectileTag))
            ok &= go.CompareTag(projectileTag);
        return ok;
    }

    private bool IsPlayerWeaponCollider(Collider2D col)
    {
        // Bỏ qua collider có/thuộc DamageSource để đòn của Player vẫn trúng Enemy
        return col != null && col.GetComponentInParent<DamageSource>() != null;
    }

    private void ApplyIgnoreCollisionPairs()
    {
        if (_ownerCols == null || _ownerCols.Length == 0) return;

        // Tìm mọi collider trong bán kính quanh Player
        var hits = Physics2D.OverlapCircleAll(owner.transform.position, radius);
        foreach (var hit in hits)
        {
            if (hit == null) continue;
            var go = hit.gameObject;
            if (go == owner) continue;
            if (!IsEnemy(go)) continue;

            // Lấy root enemy và tất cả colliders con để không bỏ sót hitbox
            var enemyRoot = go.transform.root;
            var enemyCols = enemyRoot.GetComponentsInChildren<Collider2D>(includeInactive: false);
            if (enemyCols == null || enemyCols.Length == 0) continue;

            foreach (var pCol in _ownerCols)
            {
                if (pCol == null) continue;
                if (IsPlayerWeaponCollider(pCol)) continue; // không chặn vũ khí Player

                foreach (var eCol in enemyCols)
                {
                    if (eCol == null) continue;
                    var pair = (pCol, eCol);
                    if (_ignoredPairs.Contains(pair)) continue;

                    Physics2D.IgnoreCollision(pCol, eCol, true);
                    _ignoredPairs.Add(pair);
                }
            }
        }
    }

    private void RestoreAllPairs()
    {
        foreach (var pair in _ignoredPairs)
        {
            if (pair.Item1 != null && pair.Item2 != null)
                Physics2D.IgnoreCollision(pair.Item1, pair.Item2, false);
        }
        _ignoredPairs.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsProjectile(other.gameObject))
        {
            Destroy(other.gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(owner != null ? owner.transform.position : transform.position, radius);
    }

    private void SelfDestruct()
    {
        Destroy(gameObject);
    }
}
