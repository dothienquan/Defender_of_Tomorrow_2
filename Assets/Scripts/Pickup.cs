using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio; // Cần thư viện này để dùng AudioMixer

public class Pickup : MonoBehaviour
{
    private enum PickUpType
    {
        GoldCoin,
        StaminaGlobe,
        HealthGlobe,
    }

    [SerializeField] private PickUpType pickUpType;
    [SerializeField] private float pickUpDistance = 5f;
    [SerializeField] private float accelartionRate = .2f;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private AnimationCurve animCurve;
    [SerializeField] private float heightY = 1.5f;
    [SerializeField] private float popDuration = 1f;

    [Header("Gold Coin Settings")]
    [Tooltip("Giá trị gold của coin này (chỉ áp dụng cho GoldCoin). Nếu = 0, sẽ random từ 50-200")]
    [SerializeField] private int goldValue = 0;
    [SerializeField] private int minGoldValue = 50;
    [SerializeField] private int maxGoldValue = 200;

    // --- AUDIO SETTINGS ---
    [Header("Audio Settings")]
    [Tooltip("File âm thanh khi nhặt")]
    [SerializeField] private AudioClip pickupSound;
    
    [Tooltip("Mixer Group (SFX)")]
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 1f;

    // Biến static: Chia sẻ chung cho TẤT CẢ các object Pickup
    // Lưu thời điểm tiếp theo được phép phát âm thanh
    private static float nextSoundTime = 0f;
    private const float SOUND_COOLDOWN = 1f; // Thời gian chờ 1 giây như yêu cầu
    // ----------------------

    private Vector3 moveDir;
    private Rigidbody2D rb;

    private void Awake() {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start() {
        if (pickUpType == PickUpType.GoldCoin && goldValue == 0)
        {
            goldValue = Random.Range(minGoldValue, maxGoldValue + 1); 
        }

        StartCoroutine(AnimCurveSpawnRoutine());
    }

    private void Update() {
        Vector3 playerPos = PlayerController.Instance.transform.position;

        if (Vector3.Distance(transform.position, playerPos) < pickUpDistance) {
            moveDir = (playerPos - transform.position).normalized;
            moveSpeed += accelartionRate;
        } else {
            moveDir = Vector3.zero;
            moveSpeed = 0;
        }
    }

    private void FixedUpdate() {
        rb.linearVelocity = moveDir * moveSpeed * Time.deltaTime;
    }

    private void OnTriggerStay2D(Collider2D other) {
        if (other.gameObject.GetComponent<PlayerController>()) {
            // Xử lý logic game
            DetectPickupType();
            
            // Xử lý âm thanh trước khi destroy
            PlayPickupSound();

            Destroy(gameObject);
        }
    }

    private void PlayPickupSound()
    {
        if (pickupSound == null) return;

        // Kiểm tra xem đã hết thời gian chờ chưa (Time.time là thời gian hiện tại của game)
        if (Time.time >= nextSoundTime)
        {
            // Nếu được phép phát, tạo object âm thanh tạm thời
            GameObject audioObj = new GameObject("TempAudio_Pickup");
            audioObj.transform.position = transform.position;

            AudioSource source = audioObj.AddComponent<AudioSource>();
            source.clip = pickupSound;
            source.volume = soundVolume;
            
            if (sfxMixerGroup != null)
            {
                source.outputAudioMixerGroup = sfxMixerGroup;
            }

            source.Play();
            Destroy(audioObj, pickupSound.length);

            // Cập nhật thời gian chờ cho lần tiếp theo (hiện tại + 1 giây)
            // Vì biến này là static, nó sẽ chặn tất cả các Pickup khác trong 1s tới
            nextSoundTime = Time.time + SOUND_COOLDOWN;
        }
    }

    private IEnumerator AnimCurveSpawnRoutine() {
        Vector2 startPoint = transform.position;
        float randomX = transform.position.x + Random.Range(-2f, 2f);
        float randomY = transform.position.y + Random.Range(-1f, 1f);

        Vector2 endPoint = new Vector2(randomX, randomY);

        float timePassed = 0f;

        while (timePassed < popDuration)
        {
            timePassed += Time.deltaTime;
            float linearT = timePassed / popDuration;
            float heightT = animCurve.Evaluate(linearT);
            float height = Mathf.Lerp(0f, heightY, heightT);

            transform.position = Vector2.Lerp(startPoint, endPoint, linearT) + new Vector2(0f, height);
            yield return null;
        }
    }

    private void DetectPickupType() {
        switch (pickUpType)
        {
            case PickUpType.GoldCoin:
                if (goldValue > 0)
                {
                    EconomyManager.Instance.AddGold(goldValue);
                }
                else
                {
                    int randomGold = Random.Range(minGoldValue, maxGoldValue + 1);
                    EconomyManager.Instance.AddGold(randomGold);
                }
                break;
            case PickUpType.HealthGlobe:
                PlayerHealth.Instance.HealPlayer();
                break;
            case PickUpType.StaminaGlobe:
                Stamina.Instance.RefreshStamina();
                break;
        }
    }
}