using UnityEngine;
using TMPro;

public class PassiveQuest : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI questLabel;

    private enum QuestState
    {
        Start,
        DidAction1,
        DidAction2,
        Completed
    }

    private QuestState state = QuestState.Start;

    private void Start()
    {
        UpdateText();
    }

    private void UpdateText()
    {
        switch (state)
        {
            case QuestState.Start:
                questLabel.text = "Den nha cua truong lang";
                break;
            case QuestState.DidAction1:
                questLabel.text = "Quan sat xung quanh va tim loi thoat";
                break;
            case QuestState.DidAction2:
                questLabel.text = "Xong rồi, nhiệm vụ hoàn thành.";
                break;
            case QuestState.Completed:
                questLabel.text = "Không còn nhiệm vụ nào.";
                break;
        }
    }

    public void OnAction1Done()
    {
        if (state != QuestState.Start) return;
        state = QuestState.DidAction1;
        UpdateText();
    }

    public void OnAction2Done()
    {
        if (state != QuestState.DidAction1) return;
        state = QuestState.DidAction2;
        UpdateText();
    }
}
