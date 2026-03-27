using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DialogueUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text nameText;
    public TMP_Text text;
    public Image avatarImage;

    [Header("Typing Settings")]
    public float typeSpeed = 0.03f;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip typingSound; // Kéo file âm thanh (blip/click) vào đây
    [SerializeField] private AudioSource audioSource; // Kéo AudioSource vào (hoặc để trống tự tìm)
    [Range(1, 5)] [SerializeField] private int audioFrequency = 2; // Phát âm thanh mỗi X ký tự (tránh quá ồn)
    [Range(0.5f, 2f)] [SerializeField] private float minPitch = 0.9f; // Pitch thấp nhất
    [Range(0.5f, 2f)] [SerializeField] private float maxPitch = 1.1f; // Pitch cao nhất
    [SerializeField] private bool stopAudioOnSkip = true; // Dừng âm thanh khi bấm skip

    DialogueObject currentDialogue;
    int index;
    bool isTyping;
    string currentLine;
    Coroutine typingRoutine;
    string defaultSpeakerName = "";
    Sprite defaultAvatar = null;

    private void Awake()
    {
        // Tự động tìm AudioSource nếu chưa gán
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    public void Show(DialogueObject d)
    {
        Show(d, "", null);
    }

    public void Show(DialogueObject d, string speakerName)
    {
        Show(d, speakerName, null);
    }

    public void Show(DialogueObject d, string speakerName, Sprite avatar)
    {
        currentDialogue = d;
        index = 0;
        defaultSpeakerName = speakerName;
        defaultAvatar = avatar;

        gameObject.SetActive(true);
        Next();
    }

    void Update()
    {
        if (!gameObject.activeSelf) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (currentDialogue == null) return;

            if (isTyping)
            {
                // Đang gõ dở -> hiện full câu
                if (typingRoutine != null) StopCoroutine(typingRoutine);
                text.text = currentLine;
                isTyping = false;
                
                // Dừng âm thanh khi skip
                if (stopAudioOnSkip && audioSource != null)
                {
                    audioSource.Stop();
                }
            }
            else
            {
                // Gõ xong -> sang câu tiếp theo
                Next();
            }
        }
    }

    void Next()
    {
        if (currentDialogue == null) return;

        if (index >= currentDialogue.LineCount)
        {
            currentDialogue = null;
            gameObject.SetActive(false);
            return;
        }

        DialogueLine dialogueLine = currentDialogue.GetLine(index, defaultSpeakerName, defaultAvatar);
        currentLine = dialogueLine.text;

        if (nameText != null)
            nameText.text = dialogueLine.speakerName;

        if (avatarImage != null)
        {
            if (dialogueLine.avatar != null)
            {
                avatarImage.sprite = dialogueLine.avatar;
                avatarImage.gameObject.SetActive(true);
                avatarImage.preserveAspect = true;
                avatarImage.SetNativeSize();
            }
            else
            {
                avatarImage.gameObject.SetActive(false);
            }
        }

        index++;

        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = StartCoroutine(TypeLine(currentLine));
    }

    IEnumerator TypeLine(string line)
    {
        isTyping = true;
        text.text = "";

        int charCount = 0; // Đếm số ký tự để xử lý frequency

        foreach (char c in line)
        {
            text.text += c;
            
            // --- AUDIO LOGIC ---
            if (typingSound != null && audioSource != null)
            {
                // Chỉ phát âm thanh mỗi 'audioFrequency' ký tự (ví dụ mỗi 2 ký tự kêu 1 lần)
                // Và không phát nếu là dấu cách
                if (charCount % audioFrequency == 0 && !char.IsWhiteSpace(c))
                {
                    // Random pitch để giọng không bị máy móc
                    audioSource.pitch = Random.Range(minPitch, maxPitch);
                    audioSource.PlayOneShot(typingSound);
                }
            }
            charCount++;
            // -------------------

            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false;
    }
}