using UnityEngine;
using DG.Tweening;
using UnityEngine.Audio;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance;

    [Header("Settings")]
    [SerializeField] private AudioMixerGroup bgmMixerGroup;
    [SerializeField] private float crossfadeDuration = 1f;
    [Range(0f, 1f)] [SerializeField] private float maxVolume = 1f;

    [Header("Default BGM")]
    [Tooltip("Nhạc nền mặc định của màn chơi (sẽ tự động phát khi vào game)")]
    [SerializeField] private AudioClip defaultMusic; // --- NEW: Nhạc mặc định

    private AudioSource mainSource;
    private AudioSource overrideSource;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else 
        {
            Destroy(gameObject);
            return;
        }

        mainSource = CreateSource("MainBGM_Source");
        overrideSource = CreateSource("OverrideBGM_Source");
        
        overrideSource.volume = 0f;
    }

    private void Start()
    {
        // --- NEW: Tự động phát nhạc nền mặc định nếu có ---
        if (defaultMusic != null)
        {
            PlayMainBGM(defaultMusic);
        }
    }

    private AudioSource CreateSource(string name)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(transform);
        AudioSource source = obj.AddComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = false;
        source.outputAudioMixerGroup = bgmMixerGroup;
        return source;
    }

    public void PlayMainBGM(AudioClip clip)
    {
        if (clip == null) return;
        
        // Nếu clip mới khác clip cũ thì mới đổi
        if (mainSource.clip != clip)
        {
            mainSource.clip = clip;
            mainSource.volume = maxVolume; // Reset volume về max
            mainSource.Play();
        }
        else
        {
            // Nếu clip giống nhau nhưng đang bị tắt/pause thì play lại
            if (!mainSource.isPlaying) mainSource.Play();
        }
    }

    public void StartOverrideMusic(AudioClip clip)
    {
        if (clip == null) return;

        overrideSource.clip = clip;
        overrideSource.volume = 0f; // Bắt đầu từ 0 để fade in
        overrideSource.Play();

        // Crossfade
        mainSource.DOKill();
        overrideSource.DOKill();

        mainSource.DOFade(0f, crossfadeDuration);
        overrideSource.DOFade(maxVolume, crossfadeDuration);
    }

    public void StopOverrideMusic()
    {
        // --- NEW: Đảm bảo nhạc chính đang chạy trước khi fade in ---
        if (mainSource.clip != null && !mainSource.isPlaying)
        {
            mainSource.Play();
        }

        mainSource.DOKill();
        overrideSource.DOKill();

        // Fade main lên lại & Override xuống 0
        mainSource.DOFade(maxVolume, crossfadeDuration);
        overrideSource.DOFade(0f, crossfadeDuration).OnComplete(() => 
        {
            overrideSource.Stop();
        });
    }
}