using UnityEngine;
using UnityEngine.UI;

public class RaftMiniGame : MonoBehaviour
{
    [Header("Progress Settings")]
    public int clicksToBuild = 100;
    public int currentClicks = 0;

    [Header("UI")]
    public Image raftProgressImage;   // sprite thuyền dùng làm progress bar

    [Header("Raft Spawn")]
    public GameObject raftPrefab;
    public Transform spawnPoint;

    [Header("Click VFX")]
    [Tooltip("Prefab hiệu ứng khi click (Particle System, ...).")]
    [SerializeField] private GameObject clickVfxPrefab;
    [Tooltip("Vị trí spawn VFX (nếu null sẽ dùng spawnPoint, nếu vẫn null dùng transform hiện tại).")]
    [SerializeField] private Transform vfxSpawnPoint;
    [Tooltip("Số lượng VFX trong pool.")]
    [SerializeField] private int clickVfxPoolSize = 8;

    private bool raftDone = false;

    // Pool cho VFX
    private GameObject[] _clickVfxPool;
    private ParticleSystem[] _clickVfxParticles;
    private int _clickVfxIndex = 0;

    private void Awake()
    {
        InitClickVfxPool();
    }

    private void OnEnable()
    {
        currentClicks = 0;

        if (raftProgressImage != null)
            raftProgressImage.fillAmount = 0f;
    }

    private void InitClickVfxPool()
    {
        if (clickVfxPrefab == null || clickVfxPoolSize <= 0)
            return;

        _clickVfxPool = new GameObject[clickVfxPoolSize];
        _clickVfxParticles = new ParticleSystem[clickVfxPoolSize];

        for (int i = 0; i < clickVfxPoolSize; i++)
        {
            GameObject vfx = Instantiate(clickVfxPrefab);
            vfx.SetActive(false);

            _clickVfxPool[i] = vfx;
            _clickVfxParticles[i] = vfx.GetComponent<ParticleSystem>();
        }
    }

    public void OnClickBuildButton()
    {
        if (raftDone) return;

        PlayClickVfx();

        currentClicks++;

        float progress = (float)currentClicks / clicksToBuild;

        // cập nhật thanh tiến độ (fill từ 0 → 1)
        if (raftProgressImage != null)
            raftProgressImage.fillAmount = progress;

        if (currentClicks >= clicksToBuild)
        {
            BuildRaft();
        }
    }

    private void PlayClickVfx()
    {
        if (_clickVfxPool == null || _clickVfxPool.Length == 0)
            return;

        // chọn vị trí spawn
        Vector3 pos;
        Quaternion rot;

        if (vfxSpawnPoint != null)
        {
            pos = vfxSpawnPoint.position;
            rot = vfxSpawnPoint.rotation;
        }
        else if (spawnPoint != null)
        {
            pos = spawnPoint.position;
            rot = spawnPoint.rotation;
        }
        else
        {
            pos = transform.position;
            rot = Quaternion.identity;
        }

        // lấy 1 object từ pool
        GameObject vfx = _clickVfxPool[_clickVfxIndex];
        ParticleSystem ps = _clickVfxParticles[_clickVfxIndex];

        _clickVfxIndex++;
        if (_clickVfxIndex >= _clickVfxPool.Length)
            _clickVfxIndex = 0;

        vfx.transform.SetPositionAndRotation(pos, rot);

        // reset & play particle
        if (ps != null)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            vfx.SetActive(true);
            ps.Play(true);
        }
        else
        {
            // nếu không có ParticleSystem thì chỉ bật/tắt
            vfx.SetActive(false);
            vfx.SetActive(true);
        }
    }

    private void BuildRaft()
    {
        raftDone = true;

        if (raftPrefab != null && spawnPoint != null)
            Instantiate(raftPrefab, spawnPoint.position, spawnPoint.rotation);

        gameObject.SetActive(false);

        Debug.Log("Bè đã hoàn thành!");
    }
}
