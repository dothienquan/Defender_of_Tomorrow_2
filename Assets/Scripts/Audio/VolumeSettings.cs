using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class VolumeSettings : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer mainMixer;

    [Header("UI Sliders")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    // Tên tham số bạn đã đặt trong bước Expose
    private const string MIXER_BGM = "BGM";
    private const string MIXER_SFX = "SFX";

    private void Start()
    {
        // 1. Load giá trị đã lưu (mặc định là 1 - max volume)
        float savedBGM = PlayerPrefs.GetFloat("BGM_Vol", 1f);
        float savedSFX = PlayerPrefs.GetFloat("SFX_Vol", 1f);

        // 2. Cập nhật vị trí Slider
        if (bgmSlider != null) 
        {
            bgmSlider.value = savedBGM;
            bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        }

        if (sfxSlider != null) 
        {
            sfxSlider.value = savedSFX;
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }

        // 3. Cập nhật Mixer ngay lập tức
        SetMixerVolume(MIXER_BGM, savedBGM);
        SetMixerVolume(MIXER_SFX, savedSFX);
    }

    public void SetBGMVolume(float value)
    {
        SetMixerVolume(MIXER_BGM, value);
        PlayerPrefs.SetFloat("BGM_Vol", value); // Lưu lại
    }

    public void SetSFXVolume(float value)
    {
        SetMixerVolume(MIXER_SFX, value);
        PlayerPrefs.SetFloat("SFX_Vol", value); // Lưu lại
    }

    private void SetMixerVolume(string paramName, float sliderValue)
    {
        // Audio Mixer dùng Decibel (-80dB đến 0dB)
        // Slider dùng Linear (0 đến 1)
        // Cần công thức chuyển đổi Logarithmic để âm thanh nghe tự nhiên
        
        float volumeInDB;

        if (sliderValue <= 0.001f)
        {
            volumeInDB = -80f; // Tắt tiếng hoàn toàn
        }
        else
        {
            volumeInDB = Mathf.Log10(sliderValue) * 20;
        }

        mainMixer.SetFloat(paramName, volumeInDB);
    }
}