using UnityEngine;

/// <summary>
/// Helper script để đảm bảo sprite animation chạy đúng trên UI Canvas
/// Tự động enable Animator và đảm bảo các settings đúng
/// </summary>
[RequireComponent(typeof(Animator))]
public class UISpriteAnimationHelper : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Tự động enable Animator khi Start")]
    [SerializeField] private bool autoEnableOnStart = true;

    [Tooltip("Update Mode cho Animator (mặc định: Normal)")]
    [SerializeField] private AnimatorUpdateMode updateMode = AnimatorUpdateMode.Normal;

    [Tooltip("Culling Mode cho Animator (mặc định: AlwaysAnimate)")]
    [SerializeField] private AnimatorCullingMode cullingMode = AnimatorCullingMode.AlwaysAnimate;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        
        if (animator == null)
        {
            Debug.LogError($"[UISpriteAnimationHelper] {gameObject.name} không có Animator component!");
            return;
        }

        // Đảm bảo Animator có settings đúng
        animator.updateMode = updateMode;
        animator.cullingMode = cullingMode;
    }

    private void Start()
    {
        if (animator == null) return;

        if (autoEnableOnStart)
        {
            // Đảm bảo Animator được enable
            if (!animator.enabled)
            {
                animator.enabled = true;
                Debug.Log($"[UISpriteAnimationHelper] Enabled Animator on {gameObject.name}");
            }

            // Đảm bảo Animator Controller được gán
            if (animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning($"[UISpriteAnimationHelper] {gameObject.name} không có Animator Controller được gán!");
            }
            else
            {
                // Play default state nếu có
                animator.Play(0, -1, 0f);
            }
        }
    }

    /// <summary>
    /// Enable Animator thủ công
    /// </summary>
    public void EnableAnimator()
    {
        if (animator != null)
        {
            animator.enabled = true;
        }
    }

    /// <summary>
    /// Disable Animator thủ công
    /// </summary>
    public void DisableAnimator()
    {
        if (animator != null)
        {
            animator.enabled = false;
        }
    }

    /// <summary>
    /// Kiểm tra xem Animator có đang chạy không
    /// </summary>
    public bool IsAnimatorEnabled()
    {
        return animator != null && animator.enabled;
    }

    /// <summary>
    /// Play một animation state theo tên
    /// </summary>
    public void PlayAnimation(string stateName, int layer = -1, float normalizedTime = 0f)
    {
        if (animator != null && animator.enabled)
        {
            animator.Play(stateName, layer, normalizedTime);
        }
    }

    /// <summary>
    /// Set trigger
    /// </summary>
    public void SetTrigger(string triggerName)
    {
        if (animator != null && animator.enabled)
        {
            animator.SetTrigger(triggerName);
        }
    }

    private void OnEnable()
    {
        // Khi GameObject được enable, đảm bảo Animator cũng được enable
        if (animator != null && autoEnableOnStart)
        {
            animator.enabled = true;
        }
    }

    private void OnDisable()
    {
        // Khi GameObject được disable, có thể giữ Animator enabled hoặc disable
        // Tùy vào use case, nhưng thường thì disable GameObject sẽ tự động dừng animation
    }
}

