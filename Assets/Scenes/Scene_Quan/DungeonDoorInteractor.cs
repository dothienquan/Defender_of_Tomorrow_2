
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Collider2D))]
public class DungeonDoorInteractor : MonoBehaviour
{
    [Header("References")]
    public GameObject uiPanel;
    public DoorClockController door;
    public DungeonDoorDiamondPanel diamondPanel;

    [Header("Settings")]
    public string playerTag = "Player";

    [Header("UI Hint (Optional)")]
    public GameObject hint;

    [Header("On Panel Closed Actions")]
    [SerializeField] private GameObject activateOnPanelClosed;
    [SerializeField] private Transform moveTarget;
    [SerializeField] private Vector3 moveOffset;
    [SerializeField] private bool moveInLocalSpace = false;
    [SerializeField] private GameObject vfxPrefab;

    [Tooltip("If true, these actions run only once (first time panel closes).")]
    [SerializeField] private bool runActionsOnce = true;

    [Header("Move Tween Settings")]
    [SerializeField] private float moveDuration = 0.6f;
    [SerializeField] private Ease moveEase = Ease.OutCubic;

    [Header("Shake Settings")]
    [SerializeField] private float shakeDuration = 0.6f;
    [SerializeField] private float shakeStrength = 0.08f;
    [SerializeField] private int shakeVibrato = 20;

    private bool didRunCloseActions = false;
    private bool playerInRange = false;
    private bool panelOpen = false;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Start()
    {
        if (uiPanel != null) uiPanel.SetActive(false);
        if (hint != null) hint.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = true;
            if (!panelOpen)
                OpenPanel();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;
            if (hint != null) hint.SetActive(false);
            if (panelOpen)
                ClosePanel();
        }
    }

    public void OpenPanel()
    {
        if (uiPanel == null) return;

        uiPanel.SetActive(true);
        panelOpen = true;

        if (hint != null) hint.SetActive(false);

        if (diamondPanel != null)
        {
            diamondPanel.InitializeSlots();
            diamondPanel.UpdateStatus();
        }
    }

    public void ClosePanel()
    {
        if (uiPanel == null || !panelOpen) return;

        uiPanel.SetActive(false);
        panelOpen = false;

        RunOnPanelClosedActions();

        if (playerInRange && hint != null)
            hint.SetActive(true);
    }

    private void RunOnPanelClosedActions()
    {
        if (runActionsOnce && didRunCloseActions) return;
        didRunCloseActions = true;

        if (activateOnPanelClosed != null)
            activateOnPanelClosed.SetActive(true);

        if (moveTarget != null)
        {
            moveTarget.DOKill();

            Vector3 startPos = moveInLocalSpace ? moveTarget.localPosition : moveTarget.position;
            Vector3 targetPos = startPos + moveOffset;

            Tween moveTween = moveInLocalSpace
                ? moveTarget.DOLocalMove(targetPos, moveDuration)
                : moveTarget.DOMove(targetPos, moveDuration);

            moveTween.SetEase(moveEase);

            Tween shakeTween = moveTarget.DOShakePosition(
                shakeDuration,
                shakeStrength,
                shakeVibrato,
                90f,
                false,
                false,
                ShakeRandomnessMode.Harmonic
            );

            shakeTween.OnComplete(() =>
            {
                if (activateOnPanelClosed != null)
                    activateOnPanelClosed.SetActive(false);
                if (moveTarget != null)
                    moveTarget.gameObject.SetActive(false);

                if (vfxPrefab != null)
                {
                    Vector3 spawnPosition = moveTarget != null ? moveTarget.position : transform.position;
                    GameObject vfxInstance = Instantiate(vfxPrefab, spawnPosition, Quaternion.identity);
                    Destroy(vfxInstance, 2f);
                }
            });
        }
    }

    public void OnDoorOpened()
    {
        ClosePanel();
    }
}
