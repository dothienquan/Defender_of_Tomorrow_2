using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio; // Cần thư viện này để dùng AudioMixer

public class Item : MonoBehaviour
{
    public int ID;
    public string Name;

    [Header("Raft")]
    public bool isRaftMaterial;

    [Header("Weapon")]
    [Tooltip("Nếu item này là vũ khí, gán WeaponInfo vào đây")]
    public WeaponInfo weaponInfo;

    // --- NEW: AUDIO SETTINGS ---
    [Header("Audio")]
    [Tooltip("Âm thanh khi nhặt vật phẩm")]
    [SerializeField] private AudioClip pickupSound;

    [Tooltip("Gán SFX Mixer Group vào đây để chỉnh volume chung")]
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 1f;
    // ---------------------------

    public bool IsWeapon => weaponInfo != null;
    public WeaponInfo GetWeaponInfo() => weaponInfo;

    public virtual void PickUp()
    {
        // 1. Xử lý logic hình ảnh/UI cũ
        Sprite itemIcon = null;
        Image image = GetComponent<Image>();
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (image != null) itemIcon = image.sprite;
        else if (spriteRenderer != null) itemIcon = spriteRenderer.sprite;
        else if (weaponInfo != null && weaponInfo.icon != null) itemIcon = weaponInfo.icon;
        
        if(ItemPickupUIController.Instance != null && itemIcon != null)
        {
            ItemPickupUIController.Instance.ShowItemPopup(Name, itemIcon);  
        }

        // 2. --- NEW: Phát âm thanh ---
        PlayPickupSound();
    }

    private void PlayPickupSound()
    {
        if (pickupSound == null) return;

        // Tạo một GameObject tạm thời tại vị trí của Item để phát nhạc
        // Lý do: Nếu Item bị Destroy ngay sau khi nhặt, AudioSource gắn trên nó cũng tịt luôn.
        // GameObject tạm này sẽ sống đủ lâu để phát hết âm thanh rồi tự hủy.
        GameObject audioObj = new GameObject("TempAudio_" + Name);
        audioObj.transform.position = transform.position;

        AudioSource source = audioObj.AddComponent<AudioSource>();
        source.clip = pickupSound;
        source.volume = soundVolume;
        
        // Gán Mixer Group (để ăn theo slider SFX trong Setting)
        if (sfxMixerGroup != null)
        {
            source.outputAudioMixerGroup = sfxMixerGroup;
        }

        source.Play();

        // Tự động hủy object âm thanh sau khi clip chạy xong
        Destroy(audioObj, pickupSound.length);
    }

    public virtual void UseItem()
    {
        if (!IsWeapon)
        {
            Debug.Log("Use item: " + Name);
        }
    }
}