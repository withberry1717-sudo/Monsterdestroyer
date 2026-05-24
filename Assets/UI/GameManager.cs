using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("クリアUI")]
    [SerializeField] private GameObject clearPanel;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI rankText;

    [Header("プレイ中UI")]
    [SerializeField] private TextMeshProUGUI inGameTimeText;

    private float currentTime = 0f;
    private bool isGameActive = true;

    public float CurrentTime => currentTime;
    public bool IsGameActive => isGameActive;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (clearPanel != null) clearPanel.SetActive(false);
    }

    void Update()
    {
        if (!isGameActive) return;

        currentTime += Time.deltaTime;
        UpdateInGameTimeUI();
    }

    private void UpdateInGameTimeUI()
    {
        if (inGameTimeText == null) return;
        inGameTimeText.text = FormatTime(currentTime);
    }

    public void GameClear()
    {
        if (!isGameActive) return;

        isGameActive = false;

        string niceTime = FormatTime(currentTime);

        if (timeText != null) timeText.text = "Clear Time : " + niceTime;

        if (ScoreManager.Instance != null)
        {
            string rank = ScoreManager.Instance.GetRank(currentTime);
            if (rankText != null) rankText.text = "Rank : " + rank;

            ScoreManager.Instance.ShowFinalScore(currentTime);
        }

        if (clearPanel != null) clearPanel.SetActive(true);
        if (inGameTimeText != null) inGameTimeText.gameObject.SetActive(false);
    }

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        return string.Format("{0:0}:{1:00}", minutes, seconds);
    }

    public void Stop(float duration)
    {
        if (Time.timeScale < 1f) return;
        StartCoroutine(DoHitStop(duration));
    }

    private IEnumerator DoHitStop(float duration)
    {
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void BackToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("TitleScene");
    }
}