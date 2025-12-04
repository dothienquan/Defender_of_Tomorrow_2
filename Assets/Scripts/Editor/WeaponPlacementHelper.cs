using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Helper script để đảm bảo weapon không bị snap/dịch chuyển khi đặt vào map
/// Gắn vào weapon prefab hoặc weapon instance trong scene
/// </summary>
[RequireComponent(typeof(Item))]
public class WeaponPlacementHelper : MonoBehaviour
{
    [Header("Placement Settings")]
    [Tooltip("Disable ItemMagnet khi đặt vào map (chỉ enable khi game chạy)")]
    [SerializeField] private bool disableMagnetInEditor = true;
    
    [Tooltip("Disable Rigidbody2D physics khi đặt vào map")]
    [SerializeField] private bool disablePhysicsInEditor = true;
    
    [Tooltip("Lưu vị trí ban đầu và đảm bảo không bị thay đổi khi Play")]
    [SerializeField] private bool preserveInitialPosition = true;

    private Vector3 initialPosition;
    private bool positionSaved = false;

    private void Awake()
    {
        // Chỉ chạy trong Editor
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            SetupForEditorPlacement();
        }
#endif
    }

    private void Start()
    {
        // Lưu vị trí ban đầu SAU KHI tất cả các script khác đã chạy (đặc biệt là WorldItemUIHandler)
        // Điều này đảm bảo position đã được fix nếu bị snap vào Canvas
        if (Application.isPlaying && preserveInitialPosition && !positionSaved)
        {
            Vector3 currentPos = transform.position;
            
            // Chỉ lưu position nếu không phải (0,0,0) - có thể là position hợp lệ
            if (Vector3.Distance(currentPos, Vector3.zero) > 0.01f)
            {
                initialPosition = currentPos;
                positionSaved = true;
                Debug.Log($"[WeaponPlacementHelper] {gameObject.name} saved initial position: {initialPosition}");
            }
            else
            {
                // Nếu position là (0,0,0), đợi thêm một frame để WorldItemUIHandler xử lý
                StartCoroutine(SavePositionDelayed());
            }
        }
    }

    private System.Collections.IEnumerator SavePositionDelayed()
    {
        // Đợi 2 frame để đảm bảo WorldItemUIHandler đã xử lý xong
        yield return null;
        yield return null;
        
        Vector3 currentPos = transform.position;
        if (Vector3.Distance(currentPos, Vector3.zero) > 0.01f)
        {
            initialPosition = currentPos;
            positionSaved = true;
            Debug.Log($"[WeaponPlacementHelper] {gameObject.name} saved initial position (delayed): {initialPosition}");
        }
        else
        {
            Debug.LogWarning($"[WeaponPlacementHelper] {gameObject.name} position is still (0,0,0) after delay. Cannot preserve position.");
        }
    }

    private void LateUpdate()
    {
        // Đảm bảo vị trí không bị thay đổi sau khi tất cả các script khác đã chạy
        // Chỉ check trong vài frame đầu để tránh conflict với ItemMagnet sau này
        if (Application.isPlaying && preserveInitialPosition && positionSaved && Time.frameCount < 10)
        {
            // Kiểm tra xem có bị parent vào Canvas không (có thể bị reset về 0,0,0)
            Canvas parentCanvas = transform.parent?.GetComponentInParent<Canvas>();
            if (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // Nếu bị parent vào Canvas, không restore position (để WorldItemUIHandler xử lý)
                return;
            }
            
            if (Vector3.Distance(transform.position, initialPosition) > 0.01f)
            {
                // Chỉ restore nếu position không phải (0,0,0) - có thể là do script khác
                if (Vector3.Distance(transform.position, Vector3.zero) > 0.01f)
                {
                    Debug.LogWarning($"[WeaponPlacementHelper] {gameObject.name} position was changed in frame {Time.frameCount} from {initialPosition} to {transform.position}. Restoring original position.");
                    transform.position = initialPosition;
                }
            }
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Setup weapon để không bị snap khi đặt vào map trong Editor
    /// </summary>
    private void SetupForEditorPlacement()
    {
        // Disable ItemMagnet nếu có
        if (disableMagnetInEditor)
        {
            ItemMagnet magnet = GetComponent<ItemMagnet>();
            if (magnet != null)
            {
                magnet.enabled = false;
                Debug.Log($"[WeaponPlacementHelper] Disabled ItemMagnet on {gameObject.name} for Editor placement.");
            }
        }

        // Disable Rigidbody2D physics nếu có
        if (disablePhysicsInEditor)
        {
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.simulated = false;
                Debug.Log($"[WeaponPlacementHelper] Disabled Rigidbody2D physics on {gameObject.name} for Editor placement.");
            }
        }
    }

    /// <summary>
    /// Re-enable components khi game chạy
    /// </summary>
    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            // Re-enable ItemMagnet
            ItemMagnet magnet = GetComponent<ItemMagnet>();
            if (magnet != null && disableMagnetInEditor)
            {
                magnet.enabled = true;
            }

            // Re-enable Rigidbody2D
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null && disablePhysicsInEditor)
            {
                rb.simulated = true;
            }
        }
    }
#endif
}

#if UNITY_EDITOR
/// <summary>
/// Custom Editor để dễ dàng setup weapon trong Editor
/// </summary>
[CustomEditor(typeof(WeaponPlacementHelper))]
public class WeaponPlacementHelperEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        WeaponPlacementHelper helper = (WeaponPlacementHelper)target;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Script này giúp weapon không bị snap/dịch chuyển khi đặt vào map trong Editor.", MessageType.Info);

        if (GUILayout.Button("Check Weapon Setup"))
        {
            CheckWeaponSetup(helper.gameObject);
        }
    }

    private void CheckWeaponSetup(GameObject weapon)
    {
        Debug.Log($"[WeaponPlacementHelper] Checking setup for {weapon.name}...");

        // Kiểm tra Item component
        Item item = weapon.GetComponent<Item>();
        if (item == null)
        {
            Debug.LogError($"[WeaponPlacementHelper] {weapon.name} is missing Item component!");
        }
        else
        {
            Debug.Log($"[WeaponPlacementHelper] ✓ Item component found (ID: {item.ID}, Name: {item.Name})");
        }

        // Kiểm tra ItemMagnet
        ItemMagnet magnet = weapon.GetComponent<ItemMagnet>();
        if (magnet != null)
        {
            Debug.Log($"[WeaponPlacementHelper] ✓ ItemMagnet found");
        }

        // Kiểm tra Rigidbody2D
        Rigidbody2D rb = weapon.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Debug.Log($"[WeaponPlacementHelper] ✓ Rigidbody2D found (Body Type: {rb.bodyType}, Simulated: {rb.simulated})");
        }

        // Kiểm tra Collider2D
        Collider2D collider = weapon.GetComponent<Collider2D>();
        if (collider != null)
        {
            Debug.Log($"[WeaponPlacementHelper] ✓ Collider2D found (Is Trigger: {collider.isTrigger})");
        }
        else
        {
            Debug.LogWarning($"[WeaponPlacementHelper] ⚠ {weapon.name} is missing Collider2D!");
        }

        // Kiểm tra Tag
        if (weapon.CompareTag("Item"))
        {
            Debug.Log($"[WeaponPlacementHelper] ✓ Tag 'Item' is set");
        }
        else
        {
            Debug.LogWarning($"[WeaponPlacementHelper] ⚠ {weapon.name} does not have Tag 'Item'!");
        }
    }
}
#endif

