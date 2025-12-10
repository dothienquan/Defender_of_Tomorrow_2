using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    [Tooltip("Giá trị gold tối thiểu khi random (chỉ dùng khi goldValue = 0)")]
    [SerializeField] private int minGoldValue = 50;

    [Tooltip("Giá trị gold tối đa khi random (chỉ dùng khi goldValue = 0)")]
    [SerializeField] private int maxGoldValue = 200;

    private Vector3 moveDir;
    private Rigidbody2D rb;

    private void Awake() {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start() {
        // Random gold value cho GoldCoin nếu chưa được set
        if (pickUpType == PickUpType.GoldCoin && goldValue == 0)
        {
            goldValue = Random.Range(minGoldValue, maxGoldValue + 1); // +1 vì Random.Range int exclusive max
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
            DetectPickupType();
            Destroy(gameObject);
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
                // Sử dụng goldValue đã được random hoặc set sẵn
                if (goldValue > 0)
                {
                    EconomyManager.Instance.AddGold(goldValue);
                }
                else
                {
                    // Fallback: nếu goldValue vẫn = 0, random ngay tại đây
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
