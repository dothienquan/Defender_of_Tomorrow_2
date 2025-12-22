using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlaySoundOnDisable : MonoBehaviour
{
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnDisable()
    {
        if (audioSource != null && audioSource.clip != null)
        {
            // Tạo audio tạm để không bị cắt khi object tắt
            GameObject temp = new GameObject("Temp_Audio");
            AudioSource tempSource = temp.AddComponent<AudioSource>();
            tempSource.clip = audioSource.clip;
            tempSource.volume = audioSource.volume;
            tempSource.Play();

            Destroy(temp, audioSource.clip.length);
        }
    }
}
