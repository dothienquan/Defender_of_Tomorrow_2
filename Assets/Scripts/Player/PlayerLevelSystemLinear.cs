using UnityEngine;
using System;
using UnityEngine.Audio; // Cần thư viện này để dùng AudioMixer

/// <summary>
/// Level hệ tuyến tính (linear):
/// - Max level = 30
/// - XP lên cấp: L1 -> 2 = 30, còn lại = 200
/// - Gọi AddXP(x) để cộng XP; tự xử lý lên cấp.
/// </summary>
public class PlayerLevelSystemLinear : MonoBehaviour
{
    [Header("Level & XP")]
    public int currentLevel = 1;
    public int currentXP = 0;          // XP đang tích lũy trong cấp hiện tại
    public int maxLevel = 30;

    [Header("Level Up VFX")]
    [SerializeField] private GameObject levelUpVfxPrefab; // drag prefab here
    [SerializeField] private Transform vfxSpawnPoint;      // optional, if null -> use player position
    [SerializeField] private float vfxDestroyAfter = 2f;   // seconds before auto-destroy

    [Header("Level Up Text Popup")]
    [SerializeField] private GameObject levelUpTextPrefab; // prefab with DOTween animation
    [SerializeField] private Transform textSpawnPoint;     // optional, spawn above player if null

    // --- NEW: AUDIO SETTINGS ---
    [Header("Audio Settings")]
    [Tooltip("Âm thanh khi lên cấp")]
    [SerializeField] private AudioClip levelUpSound;

    [Tooltip("Gán SFX Mixer Group")]
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    private AudioSource audioSource;
    // ---------------------------

    public event Action<int> OnLevelUp;

    // --- Properties for UI (PlayerLevelUI uses these) ---
    public int CurrentLevel => currentLevel;
    public int CurrentXP => currentXP;
    public int XpToNextLevel => GetXPToNext();

    private void Awake()
    {
        currentLevel = Mathf.Clamp(currentLevel, 1, maxLevel);

        // --- NEW: Setup AudioSource ---
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

    /// <summary> XP cần để lên cấp TIẾP THEO từ cấp hiện tại. </summary>
    public int GetXPToNext()
    {
        if (currentLevel >= maxLevel) return 0;

        if (currentLevel == 1) return 30;

        return 200;
    }

    /// <summary> Cộng XP và xử lý lên cấp (có thể lên nhiều cấp nếu XP lớn). </summary>
    public void AddXP(int amount)
    {
        if (amount <= 0 || currentLevel >= maxLevel) return;

        currentXP += amount;

        while (currentLevel < maxLevel)
        {
            int need = GetXPToNext();
            if (currentXP >= need)
            {
                currentXP -= need;
                currentLevel++;

                // --- Level up event ---
                OnLevelUp?.Invoke(currentLevel);

                // --- Spawn VFX + text + SOUND on level up ---
                SpawnLevelUpVfx();
                SpawnLevelUpText();
                PlayLevelUpSound(); // Gọi hàm phát âm thanh
            }
            else break;
        }
    }

    [ContextMenu("Force Level Up")]
    public void ForceLevelUp()
    {
        if (currentLevel >= maxLevel) return;
        currentLevel++;
        currentXP = 0;

        OnLevelUp?.Invoke(currentLevel);
        SpawnLevelUpVfx();
        SpawnLevelUpText();
        PlayLevelUpSound(); // Gọi hàm phát âm thanh
    }

    // --- NEW: Hàm phát âm thanh ---
    private void PlayLevelUpSound()
    {
        if (levelUpSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(levelUpSound, sfxVolume);
        }
    }
    // ------------------------------

    private void SpawnLevelUpVfx()
    {
        if (levelUpVfxPrefab == null) return;

        Vector3 spawnPos = (vfxSpawnPoint != null) ? vfxSpawnPoint.position : transform.position;
        Quaternion spawnRot = Quaternion.identity;

        GameObject vfx = Instantiate(levelUpVfxPrefab, spawnPos, spawnRot);

        if (vfxDestroyAfter > 0f)
        {
            Destroy(vfx, vfxDestroyAfter);
        }
    }

    private void SpawnLevelUpText()
    {
        if (levelUpTextPrefab == null) return;

        Vector3 pos = (textSpawnPoint != null)
            ? textSpawnPoint.position
            : transform.position + Vector3.up * 1.5f;

        Instantiate(levelUpTextPrefab, pos, Quaternion.identity);
    }
}