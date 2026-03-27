using UnityEngine;
using UnityEngine.UI; // Cần thiết để truy cập Button
using UnityEngine.Audio;

public class GlobalUISound : MonoBehaviour
{
    public static GlobalUISound Instance;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [Range(0f, 1f)] [SerializeField] private float volume = 1f;
    [SerializeField] private bool randomizePitch = true;

    private AudioSource audioSource;

    private void Awake()
    {
        // Singleton đơn giản để có thể gọi từ nơi khác
        if (Instance == null) Instance = this;

        // Setup AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (sfxMixerGroup != null)
        {
            audioSource.outputAudioMixerGroup = sfxMixerGroup;
        }
    }

    private void Start()
    {
        // Tự động tìm tất cả Button đang có trong Scene (kể cả inactive nếu để true)
        // Lưu ý: FindObjectsOfType khá nặng, chỉ nên dùng ở Start
        Button[] allButtons = FindObjectsByType<Button>(FindObjectsSortMode.None); 
        // Nếu dùng Unity bản cũ (<2023), dùng dòng dưới này thay thế:
        // Button[] allButtons = FindObjectsOfType<Button>();

        foreach (Button btn in allButtons)
        {
            RegisterButton(btn);
        }
    }

    /// <summary>
    /// Hàm này được gọi mỗi khi click button
    /// </summary>
    public void PlayClickSound()
    {
        if (clickSound != null && audioSource != null)
        {
            if (randomizePitch)
                audioSource.pitch = Random.Range(0.95f, 1.05f);
            else
                audioSource.pitch = 1f;

            audioSource.PlayOneShot(clickSound, volume);
        }
    }

    /// <summary>
    /// Gán âm thanh cho button (Dùng cái này cho button spawn bằng code)
    /// </summary>
    public void RegisterButton(Button btn)
    {
        if (btn == null) return;
        
        // Tránh add trùng lặp listener
        btn.onClick.RemoveListener(PlayClickSound); 
        btn.onClick.AddListener(PlayClickSound);
    }
}