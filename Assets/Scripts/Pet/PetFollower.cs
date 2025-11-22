using UnityEngine;
using DG.Tweening;

public class PetFollower : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Follow Settings")]
    [SerializeField] private float moveDuration = 0.25f;
    [SerializeField] private Vector2 baseOffset = new Vector2(-0.7f, 0.7f);

    [Header("Floating Motion")]
    [SerializeField] private float floatDistance = 0.15f;
    [SerializeField] private float floatDuration = 1.2f;

    private Tween moveTween;
    private Tween floatTween;

    private void Start()
    {
        floatTween = transform.DOMoveY(transform.position.y + floatDistance, floatDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    private void Update()
    {
        if (player == null) return;

        Vector3 targetPos = player.position + (Vector3)baseOffset;

        moveTween?.Kill();
        moveTween = transform.DOMove(targetPos, moveDuration)
            .SetEase(Ease.OutQuad);

        Vector3 scale = transform.localScale;
        if (transform.position.x < player.position.x)
            scale.x = Mathf.Abs(scale.x);      // face right
        else
            scale.x = -Mathf.Abs(scale.x);     // face left
        transform.localScale = scale;
    }

    private void OnDestroy()
    {
        moveTween?.Kill();
        floatTween?.Kill();
    }
}
