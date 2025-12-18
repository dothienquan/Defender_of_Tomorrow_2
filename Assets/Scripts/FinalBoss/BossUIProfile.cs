using UnityEngine;

[CreateAssetMenu(menuName = "UI/Boss UI Profile", fileName = "BossUIProfile")]
public class BossUIProfile : ScriptableObject
{
    [Header("Which UI to spawn for this boss type")]
    public BossHealthPanel panelPrefab;

    [Header("Optional: different slide positions per boss type")]
    public Vector2 onScreenAnchoredPos = new Vector2(0f, -40f);
    public Vector2 offScreenAnchoredPos = new Vector2(0f, 120f);

    [Header("Optional: display name (only used if your panel has a name label)")]
    public string bossDisplayName;
}
