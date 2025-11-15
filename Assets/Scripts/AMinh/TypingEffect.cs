using UnityEngine;
using TMPro;
using System.Collections;

public class TypingEffect : MonoBehaviour
{
    public TMP_Text textUI;
    [TextArea] public string fullText;
    public float charSpeed = 0.03f;

    void Start()
    {
        StartCoroutine(Type());
    }

    IEnumerator Type()
    {
        textUI.text = "";
        foreach (char c in fullText)
        {
            textUI.text += c;
            yield return new WaitForSeconds(charSpeed);
        }
    }
}
