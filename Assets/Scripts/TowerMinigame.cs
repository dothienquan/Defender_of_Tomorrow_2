using UnityEngine;
using System;
using System.Collections;
// Nếu bạn dùng VFX Graph thì uncomment dòng dưới
// using UnityEngine.VFX;

[RequireComponent(typeof(Collider2D))]
public class TowerMinigame : MonoBehaviour
{
    public event Action<TowerMinigame> OnTowerCompleted;

    [Header("Indicator (màu sắc trụ)")]
    public SpriteRenderer indicator;
    public Color idleColor = Color.white;
    public Color activeColor = Color.yellow;
    public Color doneColor = new Color(0.4f, 1f, 0.4f);

    [Header("Kích hoạt")]
    public KeyCode activateKey = KeyCode.E;
    public string playerTag = "Player";
    public bool autoActivateOnEnter = false;

    [Header("Spawn setup")]
    public Transform[] spawnPoints;
    public float spawnRadius = 4f;

    [Header("3 Waves")]
    public Wave[] waves = new Wave[3];

    [Header("VFX (dùng sẵn, không Instantiate)")]
    [Tooltip("Particle/VFX khi bắt đầu mini game (kích hoạt trụ) - gắn sẵn trong scene")]
    public ParticleSystem activateVfx;
    [Tooltip("Particle/VFX khi hoàn thành mini game - gắn sẵn trong scene")]
    public ParticleSystem completeVfx;

    // Nếu dùng VFX Graph thay vì ParticleSystem, có thể thêm:
    // public VisualEffect activateVfxGraph;
    // public VisualEffect completeVfxGraph;

    public bool IsCompleted { get; private set; }
    public bool IsActive { get; private set; }

    private int _currentWave = -1;
    private int _aliveInWave = 0;

    private Collider2D _col;
    private Coroutine _activateRoutine;

    private void Reset()
    {
        _col = GetComponent<Collider2D>();
        _col.isTrigger = true;
    }

    private void Awake()
    {
        _col = GetComponent<Collider2D>();
        if (indicator == null) indicator = GetComponentInChildren<SpriteRenderer>();

        // Tự tìm VFX nếu chưa set trong Inspector
        AutoFindVfx();

        SetIndicator(idleColor);
    }

    private void AutoFindVfx()
    {
        if (activateVfx != null && completeVfx != null) return;

        var particles = GetComponentsInChildren<ParticleSystem>(true);

        foreach (var p in particles)
        {
            var n = p.gameObject.name.ToLower();

            if (activateVfx == null && n.Contains("activate"))
            {
                activateVfx = p;
            }
            else if (completeVfx == null && n.Contains("complete"))
            {
                completeVfx = p;
            }
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (IsCompleted || IsActive) return;
        if (!other.CompareTag(playerTag)) return;

        if (autoActivateOnEnter || Input.GetKeyDown(activateKey))
        {
            Activate();
        }
    }

    public void Activate()
    {
        if (IsActive || IsCompleted) return;

        IsActive = true;
        _currentWave = -1;
        SetIndicator(activeColor);

        // Routine kích hoạt: VFX + delay 2s rồi mới chạy wave
        if (_activateRoutine != null)
        {
            StopCoroutine(_activateRoutine);
        }
        _activateRoutine = StartCoroutine(ActivateRoutine());
    }

    private IEnumerator ActivateRoutine()
    {
        // Play VFX kích hoạt có sẵn
        if (activateVfx != null)
        {
            activateVfx.transform.position = transform.position;
            activateVfx.Play();
        }

        // Nếu dùng VFX Graph:
        // if (activateVfxGraph != null)
        // {
        //     activateVfxGraph.transform.position = transform.position;
        //     activateVfxGraph.Play();
        // }

        yield return new WaitForSeconds(2f); // Delay mini game 2s

        NextWave();
        _activateRoutine = null;
    }

    private void NextWave()
    {
        _currentWave++;
        if (_currentWave >= waves.Length)
        {
            Complete();
            return;
        }

        StopAllCoroutines();
        StartCoroutine(SpawnWaveCoroutine(waves[_currentWave]));
    }

    private IEnumerator SpawnWaveCoroutine(Wave wave)
    {
        _aliveInWave = 0;
        int spawned = 0;

        while (spawned < wave.count)
        {
            Vector3 pos = GetSpawnPosition();
            var go = Instantiate(wave.enemyPrefab, pos, Quaternion.identity);

            var reporter = go.GetComponent<AutoReportOnDestroy>();
            if (reporter == null) reporter = go.AddComponent<AutoReportOnDestroy>();
            reporter.owner = this;

            _aliveInWave++;
            spawned++;
            yield return new WaitForSeconds(wave.spawnInterval);
        }
    }

    private Vector3 GetSpawnPosition()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            var p = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
            return p.position;
        }

        float ang = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        Vector3 offset = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * spawnRadius;
        return transform.position + offset;
    }

    public void NotifyUnitDestroyed()
    {
        if (!IsActive) return;

        _aliveInWave = Mathf.Max(0, _aliveInWave - 1);
        if (_aliveInWave == 0)
        {
            NextWave();
        }
    }

    private void Complete()
    {
        IsCompleted = true;
        IsActive = false;
        SetIndicator(doneColor);

        // Play VFX hoàn thành có sẵn
        if (completeVfx != null)
        {
            completeVfx.transform.position = transform.position;
            completeVfx.Play();
        }

        // Nếu dùng VFX Graph:
        // if (completeVfxGraph != null)
        // {
        //     completeVfxGraph.transform.position = transform.position;
        //     completeVfxGraph.Play();
        // }

        OnTowerCompleted?.Invoke(this);
        Debug.Log($"[TowerMinigame] Tower {name} completed.");
    }

    public void SetIndicator(Color c)
    {
        if (indicator != null) indicator.color = c;
    }
}
