using UnityEngine;
using DG.Tweening;
using UnityEngine.Audio;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance;

    [Header("Settings")]
    [SerializeField] private AudioMixerGroup bgmMixerGroup;
    [Tooltip("Thời gian chuyển nhạc (giây)")]
    [SerializeField] private float crossfadeDuration = 1.5f; // Tăng lên xíu cho mượt
    [Range(0f, 1f)] [SerializeField] private float maxVolume = 1f;

    [Header("Default BGM")]
    [SerializeField] private AudioClip defaultMusic;

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
        if (defaultMusic != null)
        {
            // Lúc mới vào game thì play luôn, không cần fade
            mainSource.clip = defaultMusic;
            mainSource.volume = maxVolume;
            mainSource.Play();
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

    // --- UPDATED: Hàm này giờ đây hỗ trợ Fade Out/In ---
    public void PlayMainBGM(AudioClip clip)
    {
        if (clip == null) return;
        if (mainSource.clip == clip) return; // Nếu nhạc giống hệt thì không làm gì

        // TRƯỜNG HỢP 1: Đang đánh Boss (Override đang chiếm sóng)
        // Chỉ cần tráo đĩa nhạc nền một cách im lặng, để khi Boss chết thì nhạc nền mới tự vang lên
        if (overrideSource.isPlaying && overrideSource.volume > 0.05f)
        {
            mainSource.clip = clip;
            if (!mainSource.isPlaying) mainSource.Play();
            return;
        }

        // TRƯỜNG HỢP 2: Chuyển vùng bình thường (Area A -> Area B)
        // Quy trình: Fade Out (50% thời gian) -> Đổi Clip -> Fade In (50% thời gian)
        mainSource.DOKill();
        
        float halfDuration = crossfadeDuration * 0.5f;

        // Bước 1: Giảm volume bài cũ xuống 0
        mainSource.DOFade(0f, halfDuration).SetEase(Ease.Linear).OnComplete(() =>
        {
            // Bước 2: Đổi bài nhạc
            mainSource.clip = clip;
            mainSource.Play();

            // Bước 3: Tăng volume bài mới lên Max
            mainSource.DOFade(maxVolume, halfDuration).SetEase(Ease.Linear);
        });
    }

    public void StartOverrideMusic(AudioClip clip)
    {
        if (clip == null) return;

        overrideSource.clip = clip;
        overrideSource.volume = 0f;
        overrideSource.Play();

        // Crossfade: Main giảm 0, Override tăng Max
        mainSource.DOKill();
        overrideSource.DOKill();

        mainSource.DOFade(0f, crossfadeDuration);
        overrideSource.DOFade(maxVolume, crossfadeDuration);
    }

    public void StopOverrideMusic()
    {
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