using UnityEngine;
using System;
using System.Collections;
using DG.Tweening;

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

    [Header("VFX (dùng sẵn trong scene, KHÔNG Instantiate)")]
    [Tooltip("Particle/VFX khi bắt đầu mini game (kích hoạt trụ) - gắn sẵn trong scene, ban đầu tắt")]
    public ParticleSystem activateVfx;
    [Tooltip("Particle/VFX khi hoàn thành mini game - gắn sẵn trong scene, ban đầu tắt")]
    public ParticleSystem completeVfx;

    // ===== PHẦN THÊM: Active object khi complete tower =====
    [Header("On Tower Completed -> Activate Object")]
    [SerializeField] private GameObject targetObject;
    [SerializeField] private ParticleSystem vfxPrefab;
    [SerializeField] private float appearDuration = 0.25f;
    [SerializeField] private Ease appearEase = Ease.OutBack;
    private bool _activatedObject;
    // ======================================================

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

        AutoFindVfx();

        // 🔹 Đảm bảo VFX ban đầu tắt hết
        if (activateVfx != null)
        {
            activateVfx.gameObject.SetActive(false);
            activateVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (completeVfx != null)
        {
            completeVfx.gameObject.SetActive(false);
            completeVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        // ===== PHẦN THÊM: đảm bảo object không hiện trước =====
        if (targetObject != null)
            targetObject.SetActive(false);
        _activatedObject = false;
        // =====================================================

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

        if (_activateRoutine != null)
            StopCoroutine(_activateRoutine);

        _activateRoutine = StartCoroutine(ActivateRoutine());
    }

    private IEnumerator ActivateRoutine()
    {
        // 🔹 Bật VFX kích hoạt
        if (activateVfx != null)
        {
            activateVfx.transform.position = transform.position;
            activateVfx.gameObject.SetActive(true);
            activateVfx.Play();
        }

        // Đảm bảo VFX hoàn thành vẫn tắt
        if (completeVfx != null)
        {
            completeVfx.gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(2f);

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

        // 🔹 Tắt VFX kích hoạt
        if (activateVfx != null)
        {
            activateVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            activateVfx.gameObject.SetActive(false);
        }

        // 🔹 Bật VFX hoàn thành
        if (completeVfx != null)
        {
            completeVfx.transform.position = transform.position;
            completeVfx.gameObject.SetActive(true);
            completeVfx.Play();
        }

        // ===== PHẦN THÊM: Active object + VFX prefab + DOTween =====
        if (!_activatedObject)
        {
            _activatedObject = true;

            if (vfxPrefab != null)
            {
                Vector3 pos = targetObject != null ? targetObject.transform.position : transform.position;
                Instantiate(vfxPrefab, pos, Quaternion.identity);
            }

            if (targetObject != null)
            {
                targetObject.transform.DOKill(true);
                targetObject.transform.localScale = Vector3.zero;
                targetObject.SetActive(true);

                targetObject.transform
                    .DOScale(Vector3.one, appearDuration)
                    .SetEase(appearEase);
            }
        }
        // ===========================================================

        OnTowerCompleted?.Invoke(this);
        Debug.Log($"[TowerMinigame] Tower {name} completed.");
    }

    public void SetIndicator(Color c)
    {
        if (indicator != null) indicator.color = c;
    }
}
