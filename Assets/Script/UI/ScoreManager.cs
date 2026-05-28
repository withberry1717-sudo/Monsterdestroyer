using TMPro;
using UnityEngine;
using System.Collections;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("In Game UI")]
    [SerializeField] private TextMeshProUGUI inGameScoreText;

    [Header("Clear Result UI")]
    [SerializeField] private TextMeshProUGUI clearTimeText;
    [SerializeField] private TextMeshProUGUI startScoreText;
    [SerializeField] private TextMeshProUGUI timePenaltyText;
    [SerializeField] private TextMeshProUGUI crystalBonusText;
    [SerializeField] private TextMeshProUGUI deathPenaltyText;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI rankText;

    [Header("スコア設定")]
    [SerializeField] private int startScore = 11200;
    [SerializeField] private float scoreDecreasePerSecond = 33f;
    [SerializeField] private int crystalBreakBonus = 1500;
    [SerializeField] private int deathPenalty = 1000;

    [Header("演出設定")]
    [SerializeField] private float showDelay = 0.35f;
    [SerializeField] private float typeInterval = 0.03f;
    [SerializeField] private float rankTypeInterval = 0.08f;
    [SerializeField] private float countDuration = 0.8f;

    private int partBonus = 0;
    private int deathCount = 0;
    private bool crystalBonusAdded = false;
    private bool resultShowing = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        UpdateInGameScoreUI();
        ResetClearResultUI();
    }

    void Update()
    {
        if (!resultShowing)
        {
            UpdateInGameScoreUI();
        }
    }

    public void AddCrystalBreakBonus()
    {
        if (crystalBonusAdded) return;

        crystalBonusAdded = true;
        partBonus += crystalBreakBonus;
        UpdateInGameScoreUI();
    }

    public void AddDeathPenalty()
    {
        deathCount++;
        UpdateInGameScoreUI();
    }

    public int GetCurrentScore()
    {
        float time = GameManager.Instance != null ? GameManager.Instance.CurrentTime : 0f;

        int score =
            startScore
            - Mathf.RoundToInt(time * scoreDecreasePerSecond)
            + partBonus
            - (deathCount * deathPenalty);

        return Mathf.Max(0, score);
    }

    public int GetFinalScore(float clearTime)
    {
        int score =
            startScore
            - Mathf.RoundToInt(clearTime * scoreDecreasePerSecond)
            + partBonus
            - (deathCount * deathPenalty);

        return Mathf.Max(0, score);
    }

    public string GetRank(float clearTime)
    {
        int score = GetFinalScore(clearTime);

        if (score >= 10000) return "S";
        if (score >= 8000) return "A";
        if (score >= 6000) return "B";
        if (score >= 4000) return "C";
        return "D";
    }

    public void ShowFinalScore(float clearTime)
    {
        if (resultShowing) return;

        resultShowing = true;
        StartCoroutine(ShowFinalScoreRoutine(clearTime));
    }

    private IEnumerator ShowFinalScoreRoutine(float clearTime)
    {
        ResetClearResultUI();

        int timePenalty = Mathf.RoundToInt(clearTime * scoreDecreasePerSecond);
        int deathTotalPenalty = deathCount * deathPenalty;
        int finalScore = GetFinalScore(clearTime);
        string rank = GetRank(clearTime);

        yield return new WaitForSeconds(showDelay);

        yield return StartCoroutine(TypeText(
            clearTimeText,
            "Clear Time      " + FormatTime(clearTime),
            typeInterval
        ));

        yield return new WaitForSeconds(showDelay);

        yield return StartCoroutine(TypeText(
            startScoreText,
            "Start Score     " + startScore,
            typeInterval
        ));

        yield return new WaitForSeconds(showDelay);

        yield return StartCoroutine(TypeText(
            timePenaltyText,
            "Time Penalty    -" + timePenalty,
            typeInterval
        ));

        yield return new WaitForSeconds(showDelay);

        yield return StartCoroutine(TypeText(
            crystalBonusText,
            "Crystal Bonus   +" + partBonus,
            typeInterval
        ));

        yield return new WaitForSeconds(showDelay);

        yield return StartCoroutine(TypeText(
            deathPenaltyText,
            "Death Penalty   -" + deathTotalPenalty,
            typeInterval
        ));

        yield return new WaitForSeconds(showDelay);

        if (finalScoreText != null)
        {
            finalScoreText.gameObject.SetActive(true);
            yield return StartCoroutine(CountFinalScore(finalScore));
        }

        yield return new WaitForSeconds(showDelay);

        yield return StartCoroutine(TypeText(
            rankText,
            "Rank                   " + rank,
            rankTypeInterval
        ));
    }

    private IEnumerator TypeText(TextMeshProUGUI targetText, string message, float interval)
    {
        if (targetText == null) yield break;

        targetText.gameObject.SetActive(true);
        targetText.text = "";

        for (int i = 0; i < message.Length; i++)
        {
            targetText.text += message[i];
            yield return new WaitForSeconds(interval);
        }
    }

    private IEnumerator CountFinalScore(int finalScore)
    {
        float timer = 0f;

        while (timer < countDuration)
        {
            timer += Time.deltaTime;
            float rate = timer / countDuration;

            int currentScore = Mathf.RoundToInt(Mathf.Lerp(0, finalScore, rate));

            if (finalScoreText != null)
            {
                finalScoreText.text = "Final Score     " + currentScore;
            }

            yield return null;
        }

        if (finalScoreText != null)
        {
            finalScoreText.text = "Final Score     " + finalScore;
        }
    }

    private void ResetClearResultUI()
    {
        if (clearTimeText != null)
        {
            clearTimeText.text = "";
            clearTimeText.gameObject.SetActive(false);
        }

        if (startScoreText != null)
        {
            startScoreText.text = "";
            startScoreText.gameObject.SetActive(false);
        }

        if (timePenaltyText != null)
        {
            timePenaltyText.text = "";
            timePenaltyText.gameObject.SetActive(false);
        }

        if (crystalBonusText != null)
        {
            crystalBonusText.text = "";
            crystalBonusText.gameObject.SetActive(false);
        }

        if (deathPenaltyText != null)
        {
            deathPenaltyText.text = "";
            deathPenaltyText.gameObject.SetActive(false);
        }

        if (finalScoreText != null)
        {
            finalScoreText.text = "";
            finalScoreText.gameObject.SetActive(false);
        }

        if (rankText != null)
        {
            rankText.text = "";
            rankText.gameObject.SetActive(false);
        }
    }

    private void UpdateInGameScoreUI()
    {
        if (inGameScoreText == null) return;

        inGameScoreText.text = "Score : " + GetCurrentScore();
    }

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);

        return minutes.ToString("00") + ":" + seconds.ToString("00");
    }
}