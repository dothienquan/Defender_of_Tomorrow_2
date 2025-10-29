using UnityEngine;
using UnityEngine.UI;
using System;

public class TimingWheel : MonoBehaviour
{
    [Header("Settings")]
    public int pointCount = 6;
    public float rotationSpeed = 90f; // degrees per second
    public float hitTolerance = 10f; // degrees tolerance for success

    [Header("References")]
    public RectTransform handTransform;
    public RectTransform pointPrefab;
    public RectTransform pointsParent;

    private float[] pointAngles;
    private bool[] pointHit;
    private float currentAngle;

    void Start()
    {
        GeneratePoints();
        currentAngle = 0f;
    }

    void Update()
    {
        RotateHand();
        CheckInput();
    }

    void GeneratePoints()
    {
        pointAngles = new float[pointCount];
        pointHit = new bool[pointCount];

        float angleStep = 360f / pointCount;

        for (int i = 0; i < pointCount; i++)
        {
            float angle = i * angleStep;
            pointAngles[i] = angle;

            RectTransform newPoint = Instantiate(pointPrefab, pointsParent);
            newPoint.localRotation = Quaternion.Euler(0, 0, -angle);
            newPoint.anchoredPosition = Vector2.zero;
        }
    }

    void RotateHand()
    {
        currentAngle += rotationSpeed * Time.deltaTime;
        if (currentAngle >= 360f) currentAngle -= 360f;
        handTransform.localRotation = Quaternion.Euler(0, 0, -currentAngle);
    }

    void CheckInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            for (int i = 0; i < pointCount; i++)
            {
                if (pointHit[i]) continue;

                float diff = Mathf.DeltaAngle(currentAngle, pointAngles[i]);
                if (Mathf.Abs(diff) <= hitTolerance)
                {
                    pointHit[i] = true;
                    Debug.Log($"Hit point {i}");
                    CheckWin();
                    break;
                }
            }
        }
    }

    void CheckWin()
    {
        foreach (bool hit in pointHit)
        {
            if (!hit) return;
        }

        Debug.Log("All points hit! Minigame complete!");
        gameObject.SetActive(false);

        // Trigger your next event here
        // Example:
        // FindObjectOfType<YourGameManager>().OnMinigameComplete();
    }
}
