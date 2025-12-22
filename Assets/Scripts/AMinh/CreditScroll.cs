using UnityEngine;
using TMPro;
using DG.Tweening;

public class CreditScroll : MonoBehaviour
{
    public TMP_Text creditText;

    public float scrollDuration = 25f;
    public float extraOffset = 500f;

    [TextArea(10, 50)]
    [SerializeField]
    private string creditContent =
@"DIRECTOR
Nguyen Van A

PROGRAMMER
Le Minh B

ART & DESIGN
Tran Thi C

SPECIAL THANKS
You ❤️
";

    Tween scrollTween;

    public void PlayCredit()
    {
        // --- THÊM DÒNG NÀY ---
        // Đưa text xuống cuối hierarchy để nó hiển thị đè lên mọi thứ khác cùng cấp
        creditText.transform.SetAsLastSibling(); 
        // ---------------------

        creditText.text = creditContent;

        RectTransform rt = creditText.rectTransform;
        rt.anchoredPosition = Vector2.zero;

        float distance = creditText.preferredHeight + extraOffset;

        scrollTween = rt
            .DOAnchorPosY(distance, scrollDuration)
            .SetEase(Ease.Linear);
    }
}