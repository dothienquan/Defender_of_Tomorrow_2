using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class BreathingLight2D : MonoBehaviour
{
    [Header("Breathing Settings")]
    [Tooltip("Cường độ ánh sáng thấp nhất")]
    [SerializeField] private float minIntensity = 0.5f;
    
    [Tooltip("Cường độ ánh sáng cao nhất")]
    [SerializeField] private float maxIntensity = 1.5f;
    
    [Tooltip("Tốc độ thở (chu kỳ mỗi giây)")]
    [SerializeField] private float breathingSpeed = 1f;
    
    [Tooltip("Độ mượt của hiệu ứng (0 = linear, 1 = smooth)")]
    [Range(0f, 1f)]
    [SerializeField] private float smoothness = 0.5f;

    [Header("Randomization (Optional)")]
    [Tooltip("Thêm độ ngẫu nhiên vào tốc độ thở")]
    [SerializeField] private bool useRandomSpeed = false;
    
    [Tooltip("Phạm vi ngẫu nhiên cho tốc độ (nhân với breathingSpeed)")]
    [SerializeField] private Vector2 randomSpeedRange = new Vector2(0.8f, 1.2f);

    private Light2D light2D;
    private float baseIntensity;
    private float time;
    private float currentSpeed;
    private float intensityRange;

    private void Awake()
    {
        light2D = GetComponent<Light2D>();
        if (light2D == null)
        {
            Debug.LogError($"BreathingLight2D: Không tìm thấy Light2D trên {gameObject.name}");
            enabled = false;
            return;
        }

        baseIntensity = light2D.intensity;
        intensityRange = maxIntensity - minIntensity;
        
        if (useRandomSpeed)
        {
            currentSpeed = breathingSpeed * Random.Range(randomSpeedRange.x, randomSpeedRange.y);
        }
        else
        {
            currentSpeed = breathingSpeed;
        }
    }

    private void Update()
    {
        time += Time.deltaTime * currentSpeed;
        
        // Sử dụng sine wave để tạo hiệu ứng thở mượt mà
        float sineValue = Mathf.Sin(time);
        
        // Chuyển từ [-1, 1] sang [0, 1]
        float normalizedValue = (sineValue + 1f) * 0.5f;
        
        // Áp dụng smoothness nếu cần
        float smoothedValue = smoothness > 0f 
            ? Mathf.SmoothStep(0f, 1f, normalizedValue) 
            : normalizedValue;
        
        // Tính intensity cuối cùng
        float targetIntensity = minIntensity + (intensityRange * smoothedValue);
        light2D.intensity = targetIntensity;
    }

    /// <summary>
    /// Đặt lại thời gian để đồng bộ với các light khác
    /// </summary>
    public void ResetTime()
    {
        time = 0f;
    }

    /// <summary>
    /// Thay đổi tốc độ thở trong runtime
    /// </summary>
    public void SetBreathingSpeed(float speed)
    {
        breathingSpeed = Mathf.Max(0f, speed);
        if (useRandomSpeed)
        {
            currentSpeed = breathingSpeed * Random.Range(randomSpeedRange.x, randomSpeedRange.y);
        }
        else
        {
            currentSpeed = breathingSpeed;
        }
    }

    /// <summary>
    /// Thay đổi phạm vi intensity trong runtime
    /// </summary>
    public void SetIntensityRange(float min, float max)
    {
        minIntensity = Mathf.Min(min, max);
        maxIntensity = Mathf.Max(min, max);
        intensityRange = maxIntensity - minIntensity;
    }

    private void OnValidate()
    {
        // Đảm bảo min <= max
        if (minIntensity > maxIntensity)
        {
            float temp = minIntensity;
            minIntensity = maxIntensity;
            maxIntensity = temp;
        }
        
        // Đảm bảo speed >= 0
        breathingSpeed = Mathf.Max(0f, breathingSpeed);
    }
}



