using UnityEngine;
using TMPro;

public class PassiveQuest : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI questLabel;

    [Header("Quest Text (Action Text)")]
    [SerializeField] [TextArea(2, 4)] private string startText = "Đến nhà của trưởng làng";
    [SerializeField] [TextArea(2, 4)] private string action1Text = "Quan sat xung quanh va tim loi thoat";
    [SerializeField] [TextArea(2, 4)] private string action2Text = "";
    [SerializeField] [TextArea(2, 4)] private string action3Text = "";
    [SerializeField] [TextArea(2, 4)] private string action4Text = "";
    [SerializeField] [TextArea(2, 4)] private string action5Text = "";
    [SerializeField] [TextArea(2, 4)] private string action6Text = "";
    [SerializeField] [TextArea(2, 4)] private string action7Text = "";
    [SerializeField] [TextArea(2, 4)] private string action8Text = "";
    [SerializeField] [TextArea(2, 4)] private string action9Text = "";
    [SerializeField] [TextArea(2, 4)] private string action10Text = "";
    [SerializeField] [TextArea(2, 4)] private string completedText = "Không còn nhiệm vụ nào.";

    private enum QuestState
    {
        Start,
        DidAction1,
        DidAction2,
        DidAction3,
        DidAction4,
        DidAction5,
        DidAction6,
        DidAction7,
        DidAction8,
        DidAction9,
        DidAction10,
        Completed
    }

    [Header("Quest Persistence")]
    [SerializeField] private string questID = "MainQuest"; // ID để lưu quest state (nếu có nhiều quest)
    [SerializeField] private bool persistQuestState = true; // Lưu quest state qua scene load

    private QuestState state = QuestState.Start;
    private string QuestStateKey => $"QuestState_{questID}";

    private void Awake()
    {
        // Load quest state nếu có
        if (persistQuestState)
        {
            LoadQuestState();
        }
    }

    private void Start()
    {
        UpdateText();
    }

    private void UpdateText()
    {
        if (questLabel == null) return;

        switch (state)
        {
            case QuestState.Start:
                questLabel.text = startText;
                break;
            case QuestState.DidAction1:
                questLabel.text = action1Text;
                break;
            case QuestState.DidAction2:
                questLabel.text = action2Text;
                break;
            case QuestState.DidAction3:
                questLabel.text = action3Text;
                break;
            case QuestState.DidAction4:
                questLabel.text = action4Text;
                break;
            case QuestState.DidAction5:
                questLabel.text = action5Text;
                break;
            case QuestState.DidAction6:
                questLabel.text = action6Text;
                break;
            case QuestState.DidAction7:
                questLabel.text = action7Text;
                break;
            case QuestState.DidAction8:
                questLabel.text = action8Text;
                break;
            case QuestState.DidAction9:
                questLabel.text = action9Text;
                break;
            case QuestState.DidAction10:
                questLabel.text = action10Text;
                break;
            case QuestState.Completed:
                questLabel.text = completedText;
                break;
        }

        // Force update text layout để đảm bảo text tự động xuống dòng
        questLabel.ForceMeshUpdate();
    }

    public void OnAction1Done()
    {
        if (state != QuestState.Start) return;
        state = QuestState.DidAction1;
        SaveQuestState();
        UpdateText();
    }

    public void OnAction2Done()
    {
        if (state != QuestState.DidAction1) return;
        state = QuestState.DidAction2;
        SaveQuestState();
        UpdateText();
    }

    public void OnAction3Done()
    {
        if (state != QuestState.DidAction2) return;
        state = QuestState.DidAction3;
        SaveQuestState();
        UpdateText();
    }

    public void OnAction4Done()
    {
        if (state != QuestState.DidAction3) return;
        state = QuestState.DidAction4;
        SaveQuestState();
        UpdateText();
    }

    public void OnAction5Done()
    {
        if (state != QuestState.DidAction4) return;
        state = QuestState.DidAction5;
        SaveQuestState();
        UpdateText();
    }

    public void OnAction6Done()
    {
        if (state != QuestState.DidAction5) return;
        state = QuestState.DidAction6;
        SaveQuestState();
        UpdateText();
    }

    public void OnAction7Done()
    {
        if (state != QuestState.DidAction6) return;
        state = QuestState.DidAction7;
        SaveQuestState();
        UpdateText();
    }

    public void OnAction8Done()
    {
        if (state != QuestState.DidAction7) return;
        state = QuestState.DidAction8;
        SaveQuestState();
        UpdateText();
    }

    public void OnAction9Done()
    {
        if (state != QuestState.DidAction8) return;
        state = QuestState.DidAction9;
        SaveQuestState();
        UpdateText();
    }

    public void OnAction10Done()
    {
        if (state != QuestState.DidAction9) return;
        state = QuestState.DidAction10;
        SaveQuestState();
        UpdateText();
    }

    public void OnQuestCompleted()
    {
        if (state != QuestState.DidAction10) return;
        state = QuestState.Completed;
        SaveQuestState();
        UpdateText();
    }

    /// <summary>
    /// Lưu quest state vào PlayerPrefs
    /// </summary>
    private void SaveQuestState()
    {
        if (!persistQuestState) return;
        
        PlayerPrefs.SetString(QuestStateKey, state.ToString());
        PlayerPrefs.Save();
        Debug.Log($"[PassiveQuest] Đã lưu quest state: {state}");
    }

    /// <summary>
    /// Load quest state từ PlayerPrefs
    /// </summary>
    private void LoadQuestState()
    {
        if (!persistQuestState) return;

        string savedState = PlayerPrefs.GetString(QuestStateKey, "");
        if (!string.IsNullOrEmpty(savedState))
        {
            if (System.Enum.TryParse<QuestState>(savedState, out QuestState loadedState))
            {
                state = loadedState;
                Debug.Log($"[PassiveQuest] Đã load quest state: {state}");
            }
            else
            {
                Debug.LogWarning($"[PassiveQuest] Không thể parse quest state: {savedState}");
            }
        }
    }

    /// <summary>
    /// Reset quest state (xóa save)
    /// </summary>
    public void ResetQuest()
    {
        state = QuestState.Start;
        if (persistQuestState)
        {
            PlayerPrefs.DeleteKey(QuestStateKey);
            PlayerPrefs.Save();
        }
        UpdateText();
        Debug.Log("[PassiveQuest] Quest đã được reset.");
    }
}
