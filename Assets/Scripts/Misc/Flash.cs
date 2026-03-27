using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flash : MonoBehaviour
{
    [Header("Flash Settings")]
    [SerializeField] private Material whiteFlashMat;
    [SerializeField] private float restoreDefaultMatTime = .2f;

    private Material defaultMat;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        defaultMat = spriteRenderer.material;
    }

    public float GetRestoreMatTime()
    {
        return restoreDefaultMatTime;
    }

    // --- Giữ hàm cũ để tương thích ---
    public IEnumerator FlashRoutine()
    {
        yield return FlashRoutine(Color.white);
    }

    // --- Overload mới cho phép đổi màu ---
    public IEnumerator FlashRoutine(Color flashColor)
    {
        if (spriteRenderer == null) yield break;

        // Dùng sharedMaterial để tránh leak instance
        spriteRenderer.material = whiteFlashMat;
        spriteRenderer.material.color = flashColor;

        yield return new WaitForSeconds(restoreDefaultMatTime);

        spriteRenderer.material = defaultMat;
    }
}
