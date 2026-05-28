using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleQuestManager : MonoBehaviour
{
    [Header("パネル")]
    [SerializeField] private GameObject questPanel;

    [Header("シーン名")]
    [SerializeField] private string battleSceneName = "BattleScene";

    private void Start()
    {
        if (questPanel != null)
        {
            questPanel.SetActive(false);
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
        SceneManager.LoadScene(battleSceneName);
    }
}