using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
public class ElectricShockWave : MonoBehaviour
{
    [Header("Wave Settings")]
    [SerializeField] private float startRadius = 0.2f;  // Bán kính ban đầu khi spawn (scale)
    [SerializeField] private float maxRadius = 3f;
    [SerializeField] private float expandDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.2f;
    [SerializeField] private int damage = 1;
    [SerializeField] private LayerMask playerLayer = 1 << 6; // Player layer
    
    [Header("Visual Settings")]
    [SerializeField] private Color waveColor = new Color(1f, 0.9f, 0.2f, 1f); // Màu vàng điện sáng
    [SerializeField] private float ringThickness = 0.15f; // Độ dày của vòng (0.1 = mỏng, 0.3 = dày)
    [SerializeField] private int sortingOrder = -1; // Hiển thị dưới slime
    [SerializeField] private string sortingLayerName = "Default";

    private SpriteRenderer _spriteRenderer;
    private CircleCollider2D _collider;
    private Transform _player;
    private Transform _slimeTransform;  // Reference đến slime để follow
    private Vector2 _offsetFromSlime = Vector2.zero;  // Offset từ slime
    private bool _hasHitPlayer;
    private Tween _expandTween;
    private Tween _fadeTween;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _collider = GetComponent<CircleCollider2D>();
        
        // Setup sprite renderer
        if (_spriteRenderer.sprite == null)
        {
            // Tạo sprite tròn đơn giản nếu không có sprite
            CreateCircleSprite();
        }
        
        // Setup sorting
        _spriteRenderer.sortingLayerName = sortingLayerName;
        _spriteRenderer.sortingOrder = sortingOrder;
        _spriteRenderer.color = waveColor;
        
        // Setup collider
        _collider.isTrigger = true;
        _collider.radius = 0.1f; // Start small
        
        // Find player
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            _player = playerObj.transform;
    }

    private void OnEnable()
    {
        // Đảm bảo position được set đúng khi enable (với offset)
        if (_slimeTransform != null)
        {
            transform.position = _slimeTransform.position + (Vector3)_offsetFromSlime;
        }
    }

    private void CreateCircleSprite()
    {
        // Tạo texture vòng tròn với viền sáng
        int size = 128; // Tăng resolution để mượt hơn
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float outerRadius = size * 0.5f - 1f;
        float innerRadius = outerRadius * (1f - ringThickness); // Vòng trong
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = 0f;
                
                // Tạo vòng tròn với viền sáng
                if (dist <= outerRadius && dist >= innerRadius)
                {
                    // Tính khoảng cách từ viền trong và viền ngoài
                    float distFromInner = dist - innerRadius;
                    float distFromOuter = outerRadius - dist;
                    float ringWidth = outerRadius - innerRadius;
                    
                    // Tạo gradient sáng ở giữa vòng, mờ ở 2 bên
                    float normalizedPos = distFromInner / ringWidth;
                    
                    // Sáng nhất ở giữa vòng, mờ dần về 2 bên
                    if (normalizedPos < 0.5f)
                    {
                        // Từ viền trong đến giữa
                        alpha = normalizedPos * 2f;
                    }
                    else
                    {
                        // Từ giữa đến viền ngoài
                        alpha = 2f * (1f - normalizedPos);
                    }
                    
                    // Làm mềm hơn
                    alpha = Mathf.Pow(alpha, 1.5f);
                    
                    // Đảm bảo viền ngoài và trong có độ sáng tối thiểu
                    float edgeFade = Mathf.Min(distFromInner, distFromOuter) / (ringWidth * 0.3f);
                    edgeFade = Mathf.Clamp01(edgeFade);
                    alpha = Mathf.Max(alpha, edgeFade * 0.3f);
                }
                
                pixels[y * size + x] = new Color(waveColor.r, waveColor.g, waveColor.b, alpha);
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        // Tạo sprite từ texture
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        _spriteRenderer.sprite = sprite;
    }

    private void Start()
    {
        PlayWaveEffect();
    }

    private void Update()
    {
        // Follow slime position mỗi frame để vòng sáng luôn ở giữa slime (với offset)
        if (_slimeTransform != null)
        {
            transform.position = _slimeTransform.position + (Vector3)_offsetFromSlime;
        }
    }

    /// <summary>
    /// Set slime transform để shockwave follow
    /// </summary>
    public void SetSlimeTransform(Transform slimeTransform, Vector2 offset = default)
    {
        _slimeTransform = slimeTransform;
        _offsetFromSlime = offset;
        if (_slimeTransform != null)
        {
            // Set position ngay lập tức với offset
            transform.position = _slimeTransform.position + (Vector3)_offsetFromSlime;
        }
    }

    public void PlayWaveEffect()
    {
        _hasHitPlayer = false;
        
        // Reset transform - bắt đầu từ startRadius để vòng sáng xuất hiện từ tâm
        transform.localScale = Vector3.one * startRadius;
        
        // Reset color với màu sáng rõ ràng
        Color color = waveColor;
        color.a = 1f;
        _spriteRenderer.color = color;
        _spriteRenderer.enabled = true;
        
        // Reset collider
        _collider.radius = 0.1f;
        _collider.enabled = true;

        // Expand scale - vòng sáng lan rộng từ từ
        _expandTween = transform.DOScale(maxRadius * 2f, expandDuration)
            .SetEase(Ease.OutQuad)
            .OnUpdate(() =>
            {
                // Đảm bảo position luôn follow slime (với offset)
                if (_slimeTransform != null)
                {
                    transform.position = _slimeTransform.position + (Vector3)_offsetFromSlime;
                }
                
                // Update collider radius based on scale
                float currentRadius = transform.localScale.x * 0.5f;
                _collider.radius = currentRadius;
                
                // Check if hit player
                if (!_hasHitPlayer && _player != null)
                {
                    // Sử dụng position của wave (đã có offset) để check
                    float distToPlayer = Vector2.Distance(transform.position, _player.position);
                    if (distToPlayer <= currentRadius)
                    {
                        HitPlayer();
                    }
                }
            })
            .OnComplete(() =>
            {
                // Fade out
                FadeOut();
            });
    }

    private void HitPlayer()
    {
        if (_hasHitPlayer) return;
        _hasHitPlayer = true;

        // Damage player using Singleton
        if (PlayerHealth.Instance != null)
        {
            PlayerHealth.Instance.TakeDamage(damage, transform);
        }

        // Stop expanding and fade out immediately
        _expandTween?.Kill();
        FadeOut();
    }

    private void FadeOut()
    {
        _collider.enabled = false;
        
        _fadeTween = _spriteRenderer.DOFade(0f, fadeOutDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                // Destroy or pool object
                Destroy(gameObject);
            });
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasHitPlayer) return;
        
        if (other.CompareTag("Player"))
        {
            HitPlayer();
        }
    }

    private void OnDestroy()
    {
        // Kill tweens to prevent errors
        _expandTween?.Kill();
        _fadeTween?.Kill();
    }
}

