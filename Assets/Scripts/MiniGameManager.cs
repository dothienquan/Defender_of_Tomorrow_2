using UnityEngine;
using System.Linq;
using DG.Tweening;

public class MiniGameManager : MonoBehaviour
{
    [Header("References (optional)")]
    public TowerMinigame[] towers;
    public Color completedColor = Color.green;

    [Header("Activate Object When MiniGame Completed")]
    [SerializeField] private GameObject targetObject;
    [SerializeField] private ParticleSystem vfxPrefab;
    [SerializeField] private float appearDuration = 0.25f;
    [SerializeField] private Ease appearEase = Ease.OutBack;

    private bool activated;

    private void Awake()
    {
        if (towers == null || towers.Length == 0)
            towers = FindObjectsOfType<TowerMinigame>(true);

        // Đảm bảo object KHÔNG hiện trước khi mini game hoàn thành
        if (targetObject != null)
            targetObject.SetActive(false);

        activated = false;
    }

    private void OnEnable()
    {
        if (towers == null) return;

        foreach (var t in towers)
        {
            if (t == null) continue;
            t.OnTowerCompleted += HandleTowerCompleted;
        }
    }

    private void OnDisable()
    {
        if (towers == null) return;

        foreach (var t in towers)
        {
            if (t == null) continue;
            t.OnTowerCompleted -= HandleTowerCompleted;
        }
    }

    private void HandleTowerCompleted(TowerMinigame t)
    {
        if (activated) return;

        bool allDone = towers != null && towers.Length > 0 && towers.All(x => x != null && x.IsCompleted);
        if (!allDone) return;

        activated = true;

        // Logic cũ: đổi indicator màu hoàn thành
        foreach (var tower in towers)
        {
            if (tower == null) continue;
            tower.SetIndicator(completedColor);
        }

        Debug.Log("[GameManager] All towers completed.");

        // Spawn VFX prefab tại vị trí target (nếu có), fallback về vị trí manager
        if (vfxPrefab != null)
        {
            Vector3 pos = targetObject != null ? targetObject.transform.position : transform.position;
            Instantiate(vfxPrefab, pos, Quaternion.identity);
        }

        // Active object + hiệu ứng xuất hiện DOTween
        if (targetObject != null)
        {
            targetObject.transform.DOKill(true);          // tránh tween chồng nếu có
            targetObject.transform.localScale = Vector3.zero;

            targetObject.SetActive(true);

            targetObject.transform
                .DOScale(Vector3.one, appearDuration)
                .SetEase(appearEase);
        }
    }
}
