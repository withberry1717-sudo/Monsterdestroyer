using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI inGameScoreText;
    [SerializeField] private TextMeshProUGUI clearScoreText;

    [Header("基本スコア")]
    [SerializeField] private int clearBaseScore = 5000;

    [Header("タイムボーナス")]
    [SerializeField] private int maxTimeBonus = 5000;
    [SerializeField] private float fastestTime = 45f;
    [SerializeField] private float bonusZeroTime = 240f;

    [Header("部位破壊ボーナス")]
    [SerializeField] private int crystalBreakBonus = 1500;

    [Header("死亡ペナルティ")]
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

    public int CalculateTimeBonus(float clearTime)
    {
        if (clearTime <= fastestTime) return maxTimeBonus;
        if (clearTime >= bonusZeroTime) return 0;

        float t = 1f - ((clearTime - fastestTime) / (bonusZeroTime - fastestTime));
        return Mathf.RoundToInt(maxTimeBonus * t);
    }

    public int GetFinalScore(float clearTime)
    {
        int timeBonus = CalculateTimeBonus(clearTime);
        int penalty = deathCount * deathPenalty;

        int finalScore = clearBaseScore + timeBonus + partBonus - penalty;
        return Mathf.Max(0, finalScore);
    }

    public string GetRank(float clearTime)
    {
        int score = GetFinalScore(clearTime);

        if (score >= 10000) return "S";
        if (score >= 8500) return "A";
        if (score >= 7000) return "B";
        if (score >= 5000) return "C";
        return "D";
    }

    public void ShowFinalScore(float clearTime)
    {
        int timeBonus = CalculateTimeBonus(clearTime);
        int penalty = deathCount * deathPenalty;
        int finalScore = GetFinalScore(clearTime);

        if (clearScoreText != null)
        {
            clearScoreText.text =
                "Score : " + finalScore +
                "\nBase : " + clearBaseScore +
                "\nTime Bonus : " + timeBonus +
                "\nCrystal Bonus : " + partBonus +
                "\nDeath Penalty : -" + penalty;
        }
    }

    private void UpdateInGameScoreUI()
    {
        if (inGameScoreText == null) return;

        int currentScore = clearBaseScore + partBonus - (deathCount * deathPenalty);
        currentScore = Mathf.Max(0, currentScore);

        inGameScoreText.text = "Score : " + currentScore;
    }
}