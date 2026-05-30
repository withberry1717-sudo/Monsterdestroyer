using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleQuestManager : MonoBehaviour
{
    [Header("パネル")]
    [SerializeField] private GameObject questPanel;

    [Header("難易度選択")]
    [Tooltip("QuestPanelについているQuestDifficultyImageSelectorを入れる。未設定でも動くが、入れておくと安全")]
    [SerializeField] private QuestDifficultyImageSelector difficultySelector;

    [Header("シーン名")]
    [SerializeField] private string battleSceneName = "BattleScene";

    private void Start()
    {
        if (questPanel != null)
        {
            questPanel.SetActive(false);
        }

        if (difficultySelector == null && questPanel != null)
        {
            difficultySelector = questPanel.GetComponent<QuestDifficultyImageSelector>();
        }
    }

    public void OpenQuestPanel()
    {
        if (questPanel != null)
        {
            questPanel.SetActive(true);
        }
    }

    public void CloseQuestPanel()
    {
        if (questPanel != null)
        {
            questPanel.SetActive(false);
        }
    }

    public void AcceptQuest()
    {
        SaveSelectedDifficulty();

        SceneManager.LoadScene(battleSceneName);
    }

    private void SaveSelectedDifficulty()
    {
        QuestDifficultyImageSelector.Difficulty selectedDifficulty =
            QuestDifficultyImageSelector.SelectedDifficulty;

        PlayerPrefs.SetInt(
            QuestDifficultyImageSelector.DifficultySaveKey,
            (int)selectedDifficulty
        );

        PlayerPrefs.Save();

        Debug.Log("Accept Quest Difficulty Saved: " + selectedDifficulty);
    }
}