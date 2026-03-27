using Cinemachine;
using UnityEngine;
using DG.Tweening;
using System.Collections;

public class MapTransition : MonoBehaviour
{
    [SerializeField] private PolygonCollider2D mapBoundry;
    [SerializeField] private TransitionType transitionType = TransitionType.NormalMove;
    [SerializeField] private Direction direction;
    [SerializeField] private Transform teleportTargetPosition;

    [Header("Portal Settings")]
    [SerializeField] private float suckDuration = 0.4f;
    [SerializeField] private float waitDuration = 1.5f;
    [SerializeField] private float zoomOutDuration = 0.35f;

    [Header("Fade Settings")]
    [SerializeField] private bool useFadeEffect = true;
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float waitBetweenFades = 0.3f;

    [SerializeField] private CinemachineConfiner confiner;

    private enum Direction { Up, Down, Left, Right, Teleport }
    private enum TransitionType { NormalMove, Portal }

    private void Awake()
    {
        if (confiner == null)
            confiner = FindFirstObjectByType<CinemachineConfiner>();

        if (confiner == null)
            Debug.LogError("Không tìm thấy CinemachineConfiner.");

        if (mapBoundry == null)
            Debug.LogError("Chưa gán mapBoundry.");
    }

    private void Start()
    {
        if (confiner != null && mapBoundry != null)
            SetupConfiner();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        if (transitionType == TransitionType.Portal)
        {
            StartCoroutine(PortalTransition(collision.gameObject));
            return;
        }

        // Chỉ setup confiner cho NormalMove transition (không phải Portal)
        if (confiner != null && mapBoundry != null)
            SetupConfiner();

        // Sử dụng fade effect cho NormalMove transition
        if (useFadeEffect)
        {
            StartCoroutine(NormalMoveWithFade(collision.gameObject));
        }
        else
        {
            UpdatePlayerPosition(collision.gameObject);
        }
    }

    private void SetupConfiner()
    {
        confiner.m_ConfineMode = CinemachineConfiner.Mode.Confine2D;
        confiner.m_BoundingShape2D = mapBoundry;
        confiner.InvalidatePathCache();
    }

    private IEnumerator PortalTransition(GameObject player)
    {
        if (teleportTargetPosition == null)
        {
            Debug.LogError("Portal mode requires teleportTargetPosition.");
            yield break;
        }

        // Disable player movement
        PlayerController playerController = player.GetComponent<PlayerController>();
        bool wasEnabled = playerController != null && playerController.enabled;
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        // BƯỚC 1: Delay 1 giây sau khi chạm trigger
        yield return new WaitForSeconds(1f);

        // Tạm thời disable camera follow và confiner để camera không di chuyển khi teleport
        CinemachineVirtualCamera virtualCamera = FindFirstObjectByType<CinemachineVirtualCamera>();
        Transform originalFollow = null;
        if (virtualCamera != null)
        {
            originalFollow = virtualCamera.Follow;
            virtualCamera.Follow = null; // Disable camera follow
        }

        // Disable confiner để camera không snap vào boundary mới
        if (confiner != null)
        {
            confiner.enabled = false;
        }

        // BƯỚC 2: Fade in (fade to black) - che màn hình trước khi teleport
        if (useFadeEffect && UIFade.Instance != null)
        {
            UIFade.Instance.FadeToBlack(fadeInDuration);
            yield return new WaitForSeconds(fadeInDuration);
        }

        // BƯỚC 3: Teleport player KHI MÀN HÌNH ĐÃ ĐEN (sau fade in)
        player.transform.position = teleportTargetPosition.position;
        Debug.Log($"[MapTransition] Đã teleport player đến: {teleportTargetPosition.position} (khi màn hình đã đen)");

        // Setup confiner sau khi teleport (vẫn khi màn hình đen)
        if (confiner != null && mapBoundry != null)
        {
            confiner.enabled = true;
            SetupConfiner();
            Debug.Log("[MapTransition] Đã setup confiner (khi màn hình đã đen)");
        }

        // Re-enable camera follow sau khi teleport và setup confiner (vẫn khi màn hình đen)
        if (virtualCamera != null && originalFollow != null)
        {
            virtualCamera.Follow = originalFollow;
            Debug.Log("[MapTransition] Đã re-enable camera follow (khi màn hình đã đen)");
        }

        // BƯỚC 4: Fade out (fade to clear) - hiện cảnh mới SAU KHI đã teleport
        if (useFadeEffect && UIFade.Instance != null)
        {
            UIFade.Instance.FadeToClear(fadeOutDuration);
            yield return new WaitForSeconds(fadeOutDuration);
        }

        // Re-enable player movement sau khi fade out xong
        if (playerController != null && wasEnabled)
        {
            playerController.enabled = true;
            Debug.Log("[MapTransition] Đã re-enable player movement");
        }
    }


    /// <summary>
    /// Normal move transition với fade effect
    /// </summary>
    private IEnumerator NormalMoveWithFade(GameObject player)
    {
        // Disable player movement tạm thời
        PlayerController playerController = player.GetComponent<PlayerController>();
        bool wasEnabled = playerController != null && playerController.enabled;
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        // Fade to black
        if (UIFade.Instance != null)
        {
            UIFade.Instance.FadeToBlack(fadeInDuration);
            yield return new WaitForSeconds(fadeInDuration);
        }

        // Wait a bit
        yield return new WaitForSeconds(waitBetweenFades);

        // Update player position
        UpdatePlayerPosition(player);

        // Setup confiner sau khi di chuyển
        if (confiner != null && mapBoundry != null)
        {
            SetupConfiner();
        }

        // Fade to clear
        if (UIFade.Instance != null)
        {
            UIFade.Instance.FadeToClear(fadeOutDuration);
            yield return new WaitForSeconds(fadeOutDuration);
        }

        // Re-enable player movement
        if (playerController != null && wasEnabled)
        {
            playerController.enabled = true;
        }
    }

    private void UpdatePlayerPosition(GameObject player)
    {
        if (direction == Direction.Teleport)
        {
            if (teleportTargetPosition == null)
            {
                Debug.LogError("Teleport mode needs teleportTargetPosition.");
                return;
            }

            player.transform.position = teleportTargetPosition.position;
            return;
        }

        var pos = player.transform.position;
        switch (direction)
        {
            case Direction.Up: pos.y += 2f; break;
            case Direction.Down: pos.y -= 2f; break;
            case Direction.Left: pos.x -= 2f; break;
            case Direction.Right: pos.x += 2f; break;
        }
        player.transform.position = pos;
    }
}
