using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI inGameScoreText;
    [SerializeField] private TextMeshProUGUI clearScoreText;

    [Header("ƒXƒRƒAÝ’è")]
    [SerializeField] private int startScore = 11200;
    [SerializeField] private float scoreDecreasePerSecond = 33f;
    [SerializeField] private int crystalBreakBonus = 1500;
    [SerializeField] private int deathPenalty = 1000;

    private int partBonus = 0;
    private int deathCount = 0;
    private bool crystalBonusAdded = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        UpdateInGameScoreUI();
    }

    void Update()
    {
        UpdateInGameScoreUI();
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
        int timePenalty = Mathf.RoundToInt(clearTime * scoreDecreasePerSecond);
        int deathTotalPenalty = deathCount * deathPenalty;
        int finalScore = GetFinalScore(clearTime);

        if (clearScoreText != null)
        {
            clearScoreText.text =
                "Score : " + finalScore +
                "\nStart : " + startScore +
                "\nTime Penalty : -" + timePenalty +
                "\nCrystal Bonus : +" + partBonus +
                "\nDeath Penalty : -" + deathTotalPenalty;
        }
    }

    private void UpdateInGameScoreUI()
    {
        if (inGameScoreText == null) return;

        inGameScoreText.text = "Score : " + GetCurrentScore();
    }
}