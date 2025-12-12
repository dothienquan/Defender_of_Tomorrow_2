using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using TMPro;

public enum BossDetectionMode
{
    None,               // Không tự động phát hiện
    ByName,             // Tìm theo tên object (một hoặc nhiều tên)
    ByTag,              // Tìm theo tag
    ByComponent,        // Tìm tất cả EnemyHealth trong scene
    FirstEnemyHealth    // Tìm EnemyHealth đầu tiên có trong scene
}

public class BossHPUI : MonoBehaviour
{
    [Header("References")]
    public Slider hpSlider;
    public EnemyHealth bossHealth;
    public TextMeshProUGUI hpText;

    [Header("Auto Detection")]
    [Tooltip("Phương thức phát hiện boss tự động")]
    [SerializeField] private BossDetectionMode detectionMode = BossDetectionMode.None;
    
    [Tooltip("Tên object(s) cần tìm (dùng khi Detection Mode = ByName). Có thể nhập nhiều tên, mỗi tên một dòng hoặc cách nhau bởi dấu phẩy.")]
    [SerializeField] private string[] bossObjectNames = new string[0];
    
    [Tooltip("Tag cần tìm (dùng khi Detection Mode = ByTag)")]
    [SerializeField] private string bossTag = "Enemy";
    
    [Tooltip("Khoảng thời gian (giây) giữa các lần check. Nhỏ hơn = check thường xuyên hơn nhưng tốn performance hơn.")]
    [SerializeField] private float detectionCheckInterval = 0.5f;

    private FieldInfo fi_current;
    private FieldInfo fi_start;

    private bool active = false;
    private float lastCheckTime = 0f;
    private EnemyHealth manuallyAssignedBoss = null;  // Boss được gán thủ công (qua Inspector hoặc code)
    private bool isManuallyAssigned = false;          // Flag để biết boss hiện tại có phải gán thủ công không

    void Awake()
    {
        gameObject.SetActive(false);

        var t = typeof(EnemyHealth);
        fi_current = t.GetField("currentHealth", BindingFlags.NonPublic | BindingFlags.Instance);
        fi_start = t.GetField("startingHealth", BindingFlags.NonPublic | BindingFlags.Instance);

        // Lưu boss được gán thủ công ban đầu
        if (bossHealth != null)
        {
            manuallyAssignedBoss = bossHealth;
            isManuallyAssigned = true;
        }
    }

    void Update()
    {
        // Kiểm tra nếu boss được gán thủ công biến mất hoặc inactive
        if (isManuallyAssigned)
        {
            if (manuallyAssignedBoss == null)
            {
                // Boss được gán thủ công đã biến mất (bị destroy), reset flag và tìm boss tiếp theo
                isManuallyAssigned = false;
                bossHealth = null;
                HideBossHP();
            }
            else
            {
                // Kiểm tra xem object có còn tồn tại không
                bool objectExists = manuallyAssignedBoss.gameObject != null;
                bool objectActive = objectExists && manuallyAssignedBoss.gameObject.activeInHierarchy;
                
                if (!objectExists)
                {
                    // Object đã bị destroy nhưng reference vẫn còn (Unity chưa set null)
                    isManuallyAssigned = false;
                    bossHealth = null;
                    HideBossHP();
                }
                else if (!objectActive)
                {
                    // Boss được gán thủ công nhưng inactive
                    bossHealth = null;
                    HideBossHP();
                }
                else if (bossHealth == null)
                {
                    // Boss được gán thủ công giờ đã active, gán lại và hiển thị
                    bossHealth = manuallyAssignedBoss;
                    ShowBossHP();
                }
            }
        }

        // Kiểm tra nếu boss hiện tại bị destroy hoặc null
        if (active && bossHealth == null)
        {
            HideBossHP();
        }

        // Kiểm tra nếu boss hiện tại chết (máu <= 0) và là boss được gán thủ công
        if (active && bossHealth != null && isManuallyAssigned && bossHealth == manuallyAssignedBoss)
        {
            try
            {
                int currentHealth = (int)fi_current.GetValue(bossHealth);
                if (currentHealth <= 0)
                {
                    // Boss được gán thủ công đã chết, reset để tìm boss tiếp theo
                    isManuallyAssigned = false;
                    bossHealth = null;
                    HideBossHP();
                }
            }
            catch
            {
                // Nếu không thể đọc được health (object đã destroy), reset
                isManuallyAssigned = false;
                bossHealth = null;
                HideBossHP();
            }
        }

        // Auto detection: tìm boss theo phương thức đã chọn
        // Tìm khi: chưa active, chưa có bossHealth, và đã set detection mode
        // VÀ (chưa có boss được gán thủ công HOẶC boss được gán thủ công đã biến mất/inactive/chết)
        bool manuallyAssignedButInactiveOrDead = isManuallyAssigned && 
                                                 (manuallyAssignedBoss == null || 
                                                  manuallyAssignedBoss.gameObject == null || 
                                                  !manuallyAssignedBoss.gameObject.activeInHierarchy);
        
        bool shouldSearch = !active && bossHealth == null && detectionMode != BossDetectionMode.None &&
                           (!isManuallyAssigned || manuallyAssignedButInactiveOrDead);

        if (shouldSearch)
        {
            if (Time.time - lastCheckTime >= detectionCheckInterval)
            {
                lastCheckTime = Time.time;
                TryFindBoss();
            }
        }

        if (!active || bossHealth == null) return;

        int current = (int)fi_current.GetValue(bossHealth);
        int start = (int)fi_start.GetValue(bossHealth);

        hpSlider.value = Mathf.Clamp01((float)current / start);

        // UPDATE TEXT
        hpText.text = current + " / " + start;

        if (current <= 0)
        {
            HideBossHP();
        }
    }

    private void TryFindBoss()
    {
        EnemyHealth foundHealth = null;

        switch (detectionMode)
        {
            case BossDetectionMode.ByName:
                foundHealth = FindBossByName();
                break;

            case BossDetectionMode.ByTag:
                foundHealth = FindBossByTag();
                break;

            case BossDetectionMode.ByComponent:
                foundHealth = FindBossByComponent();
                break;

            case BossDetectionMode.FirstEnemyHealth:
                foundHealth = FindFirstEnemyHealth();
                break;
        }

        if (foundHealth != null)
        {
            // Nếu tìm thấy boss được gán thủ công (giờ đã active), dùng lại nó
            if (isManuallyAssigned && foundHealth == manuallyAssignedBoss)
            {
                bossHealth = foundHealth;
                ShowBossHP();
            }
            // Nếu tìm thấy boss khác (không phải boss đã gán thủ công), dùng boss mới
            else
            {
                bossHealth = foundHealth;
                isManuallyAssigned = false; // Boss mới này là tự động tìm thấy
                ShowBossHP();
            }
        }
    }

    private EnemyHealth FindBossByName()
    {
        if (bossObjectNames == null || bossObjectNames.Length == 0) return null;

        foreach (string name in bossObjectNames)
        {
            if (string.IsNullOrEmpty(name)) continue;

            GameObject bossObj = GameObject.Find(name);
            if (bossObj != null && bossObj.activeInHierarchy)
            {
                EnemyHealth health = bossObj.GetComponent<EnemyHealth>();
                if (health != null)
                {
                    // Nếu là boss được gán thủ công và đã active, trả về nó
                    if (isManuallyAssigned && health == manuallyAssignedBoss)
                        return health;
                    // Nếu không phải boss được gán thủ công (hoặc boss đã chết), trả về boss mới
                    else if (health != manuallyAssignedBoss)
                        return health;
                }
            }
        }

        return null;
    }

    private EnemyHealth FindBossByTag()
    {
        if (string.IsNullOrEmpty(bossTag)) return null;

        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(bossTag);
        foreach (GameObject obj in taggedObjects)
        {
            if (obj == null || !obj.activeInHierarchy) continue;
            
            EnemyHealth health = obj.GetComponent<EnemyHealth>();
            if (health != null)
            {
                // Nếu là boss được gán thủ công và đã active, trả về nó
                if (isManuallyAssigned && health == manuallyAssignedBoss)
                    return health;
                // Nếu không phải boss được gán thủ công (hoặc boss đã chết), trả về boss mới
                else if (health != manuallyAssignedBoss)
                    return health;
            }
        }

        return null;
    }

    private EnemyHealth FindBossByComponent()
    {
        EnemyHealth[] allEnemyHealths = FindObjectsOfType<EnemyHealth>();
        if (allEnemyHealths != null && allEnemyHealths.Length > 0)
        {
            // Ưu tiên object có tên trong danh sách nếu có
            if (bossObjectNames != null && bossObjectNames.Length > 0)
            {
                foreach (string name in bossObjectNames)
                {
                    if (string.IsNullOrEmpty(name)) continue;
                    foreach (EnemyHealth health in allEnemyHealths)
                    {
                        if (health == null || health.gameObject == null || !health.gameObject.activeInHierarchy)
                            continue;
                            
                        if (health.gameObject.name == name)
                        {
                            // Nếu là boss được gán thủ công và đã active, trả về nó
                            if (isManuallyAssigned && health == manuallyAssignedBoss)
                                return health;
                            // Nếu không phải boss được gán thủ công (hoặc boss đã chết), trả về boss mới
                            else if (health != manuallyAssignedBoss)
                                return health;
                        }
                    }
                }
            }

            // Nếu không tìm thấy theo tên, trả về cái đầu tiên active
            foreach (EnemyHealth health in allEnemyHealths)
            {
                if (health == null || health.gameObject == null || !health.gameObject.activeInHierarchy)
                    continue;
                    
                // Nếu là boss được gán thủ công và đã active, trả về nó
                if (isManuallyAssigned && health == manuallyAssignedBoss)
                    return health;
                // Nếu không phải boss được gán thủ công (hoặc boss đã chết), trả về boss mới
                else if (health != manuallyAssignedBoss)
                    return health;
            }
        }

        return null;
    }

    private EnemyHealth FindFirstEnemyHealth()
    {
        EnemyHealth[] allEnemyHealths = FindObjectsOfType<EnemyHealth>();
        if (allEnemyHealths != null && allEnemyHealths.Length > 0)
        {
            // Tìm boss đầu tiên active
            foreach (EnemyHealth health in allEnemyHealths)
            {
                if (health == null || health.gameObject == null || !health.gameObject.activeInHierarchy)
                    continue;
                    
                // Nếu là boss được gán thủ công và đã active, trả về nó
                if (isManuallyAssigned && health == manuallyAssignedBoss)
                    return health;
                // Nếu không phải boss được gán thủ công (hoặc boss đã chết), trả về boss mới
                else if (health != manuallyAssignedBoss)
                    return health;
            }
        }
        return null;
    }

    public void ShowBossHP()
    {
        active = true;
        gameObject.SetActive(true);
    }

    public void HideBossHP()
    {
        active = false;
        gameObject.SetActive(false);
    }
}
