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

    [System.Serializable]
    public class DifficultyScoreSetting
    {
        [Header("難易度")]
        public QuestDifficultyImageSelector.Difficulty difficulty;

        [Header("基本スコア")]
        [Tooltip("ゲーム開始時のスコア")]
        public int startScore = 12000;

        [Tooltip("1秒ごとに減るスコア")]
        public float scoreDecreasePerSecond = 36f;

        [Tooltip("尻尾・結晶破壊ボーナス")]
        public int crystalBreakBonus = 500;

        [Tooltip("1回死亡ごとのペナルティ")]
        public int deathPenalty = 300;

        [Header("ランクしきい値")]
        [Tooltip("このスコア以上ならS")]
        public int sRankScore = 10000;

        [Tooltip("このスコア以上ならA")]
        public int aRankScore = 8000;

        [Tooltip("このスコア以上ならB")]
        public int bRankScore = 6000;

        [Tooltip("このスコア以上ならC")]
        public int cRankScore = 4000;
    }

    [Header("難易度別スコア設定")]
    [SerializeField]
    private DifficultyScoreSetting easySetting = new DifficultyScoreSetting
    {
        difficulty = QuestDifficultyImageSelector.Difficulty.Easy,
        startScore = 9600,
        scoreDecreasePerSecond = 24f,
        crystalBreakBonus = 500,
        deathPenalty = 0,
        sRankScore = 8500,
        aRankScore = 7200,
        bRankScore = 5600,
        cRankScore = 4000
    };

    [SerializeField]
    private DifficultyScoreSetting normalSetting = new DifficultyScoreSetting
    {
        difficulty = QuestDifficultyImageSelector.Difficulty.Normal,
        startScore = 12000,
        scoreDecreasePerSecond = 36f,
        crystalBreakBonus = 500,
        deathPenalty = 300,
        sRankScore = 10000,
        aRankScore = 8000,
        bRankScore = 6000,
        cRankScore = 4000
    };

    [SerializeField]
    private DifficultyScoreSetting hardSetting = new DifficultyScoreSetting
    {
        difficulty = QuestDifficultyImageSelector.Difficulty.Hard,
        startScore = 14400,
        scoreDecreasePerSecond = 44f,
        crystalBreakBonus = 500,
        deathPenalty = 1000,
        sRankScore = 12000,
        aRankScore = 9500,
        bRankScore = 7000,
        cRankScore = 4500
    };

    [Header("演出設定")]
    [SerializeField] private float showDelay = 0.35f;
    [SerializeField] private float typeInterval = 0.03f;
    [SerializeField] private float rankTypeInterval = 0.08f;
    [SerializeField] private float countDuration = 0.8f;

    [Header("Result SE")]
    [Tooltip("リザルト表示用AudioSource。空なら自分か子から自動取得します。")]
    [SerializeField] private AudioSource resultAudioSource;

    [Tooltip("Final ScoreとRank以外の項目が出る時に鳴らす共通SEです。")]
    [SerializeField] private AudioClip commonLineSfx;

    [Tooltip("Final Scoreが出る時に鳴らすSEです。空ならCommon Line Sfxを使います。")]
    [SerializeField] private AudioClip finalScoreSfx;

    [Tooltip("Rank Textが出る時に鳴らすSEです。空ならCommon Line Sfxを使います。")]
    [SerializeField] private AudioClip rankSfx;

    [Range(0f, 1f)]
    [Tooltip("リザルトSEの音量です。")]
    [SerializeField] private float resultSfxVolume = 1f;

    [Header("Result SE Timing")]
    [Tooltip("Clear Time表示開始からSEを鳴らすまでの遅延秒数です。")]
    [SerializeField] private float clearTimeSfxDelay = 0f;

    [Tooltip("Start Score表示開始からSEを鳴らすまでの遅延秒数です。")]
    [SerializeField] private float startScoreSfxDelay = 0f;

    [Tooltip("Time Penalty表示開始からSEを鳴らすまでの遅延秒数です。")]
    [SerializeField] private float timePenaltySfxDelay = 0f;

    [Tooltip("Crystal Bonus表示開始からSEを鳴らすまでの遅延秒数です。")]
    [SerializeField] private float crystalBonusSfxDelay = 0f;

    [Tooltip("Death Penalty表示開始からSEを鳴らすまでの遅延秒数です。")]
    [SerializeField] private float deathPenaltySfxDelay = 0f;

    [Tooltip("Final Score表示開始からSEを鳴らすまでの遅延秒数です。")]
    [SerializeField] private float finalScoreSfxDelay = 0f;

    [Tooltip("Rank Text表示開始からSEを鳴らすまでの遅延秒数です。")]
    [SerializeField] private float rankSfxDelay = 0f;

    [Header("デバッグ")]
    [SerializeField] private bool showDifficultyInConsole = true;

    private DifficultyScoreSetting currentSetting;

    private int partBonus = 0;
    private int deathCount = 0;
    private bool crystalBonusAdded = false;
    private bool resultShowing = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        ApplyDifficultyScoreSetting();
        FindResultAudioSourceIfNeeded();
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

    private void ApplyDifficultyScoreSetting()
    {
        QuestDifficultyImageSelector.Difficulty difficulty =
            QuestDifficultyImageSelector.LoadSavedDifficulty();

        switch (difficulty)
        {
            case QuestDifficultyImageSelector.Difficulty.Easy:
                currentSetting = easySetting;
                break;

            case QuestDifficultyImageSelector.Difficulty.Normal:
                currentSetting = normalSetting;
                break;

            case QuestDifficultyImageSelector.Difficulty.Hard:
                currentSetting = hardSetting;
                break;

            default:
                currentSetting = normalSetting;
                break;
        }

        if (showDifficultyInConsole && currentSetting != null)
        {
            Debug.Log(
                "Score Difficulty Applied: " + currentSetting.difficulty +
                " / StartScore: " + currentSetting.startScore +
                " / DecreasePerSecond: " + currentSetting.scoreDecreasePerSecond +
                " / DeathPenalty: " + currentSetting.deathPenalty +
                " / CrystalBonus: " + currentSetting.crystalBreakBonus
            );
        }
    }

    public void AddCrystalBreakBonus()
    {
        if (crystalBonusAdded) return;

        crystalBonusAdded = true;
        partBonus += GetCrystalBreakBonus();
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
            GetStartScore()
            - Mathf.RoundToInt(time * GetScoreDecreasePerSecond())
            + partBonus
            - (deathCount * GetDeathPenalty());

        return Mathf.Max(0, score);
    }

    public int GetFinalScore(float clearTime)
    {
        int score =
            GetStartScore()
            - Mathf.RoundToInt(clearTime * GetScoreDecreasePerSecond())
            + partBonus
            - (deathCount * GetDeathPenalty());

        return Mathf.Max(0, score);
    }

    public string GetRank(float clearTime)
    {
        int score = GetFinalScore(clearTime);

        if (score >= GetSRankScore()) return "S";
        if (score >= GetARankScore()) return "A";
        if (score >= GetBRankScore()) return "B";
        if (score >= GetCRankScore()) return "C";
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

        int timePenalty = Mathf.RoundToInt(clearTime * GetScoreDecreasePerSecond());
        int deathTotalPenalty = deathCount * GetDeathPenalty();
        int finalScore = GetFinalScore(clearTime);
        string rank = GetRank(clearTime);

        yield return new WaitForSeconds(showDelay);

        yield return StartCoroutine(TypeText(
            clearTimeText,
            "Clear Time      " + FormatTime(clearTime),
            typeInterval,
            commonLineSfx,
            clearTimeSfxDelay
        ));

        yield return new WaitForSeconds(showDelay);

        yield return StartCoroutine(TypeText(
            startScoreText,
            "Start Score     " + GetStartScore(),
            typeInterval,
            commonLineSfx,
            startScoreSfxDelay
        ));

        yield return new WaitForSeconds(showDelay);

        yield return StartCoroutine(TypeText(
            timePenaltyText,
            "Time Penalty    -" + timePenalty,
            typeInterval,
            commonLineSfx,
            timePenaltySfxDelay
        ));

        yield return new WaitForSeconds(showDelay);

        yield return StartCoroutine(TypeText(
            crystalBonusText,
            "Crystal Bonus   +" + partBonus,
            typeInterval,
            commonLineSfx,
            crystalBonusSfxDelay
        ));

        yield return new WaitForSeconds(showDelay);

        yield return StartCoroutine(TypeText(
            deathPenaltyText,
            "Death Penalty   -" + deathTotalPenalty,
            typeInterval,
            commonLineSfx,
            deathPenaltySfxDelay
        ));

        yield return new WaitForSeconds(showDelay);

        if (finalScoreText != null)
        {
            finalScoreText.gameObject.SetActive(true);
            PlayResultSfx(finalScoreSfx != null ? finalScoreSfx : commonLineSfx, finalScoreSfxDelay);
            yield return StartCoroutine(CountFinalScore(finalScore));
        }

        yield return new WaitForSeconds(showDelay);

        yield return StartCoroutine(TypeText(
            rankText,
            "Rank                   " + rank,
            rankTypeInterval,
            rankSfx != null ? rankSfx : commonLineSfx,
            rankSfxDelay
        ));
    }

    private IEnumerator TypeText(
        TextMeshProUGUI targetText,
        string message,
        float interval,
        AudioClip appearSfx,
        float appearSfxDelay
    )
    {
        if (targetText == null) yield break;

        targetText.gameObject.SetActive(true);
        targetText.text = "";

        PlayResultSfx(appearSfx, appearSfxDelay);

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

    private void FindResultAudioSourceIfNeeded()
    {
        if (resultAudioSource != null) return;

        resultAudioSource = GetComponent<AudioSource>();

        if (resultAudioSource == null)
        {
            resultAudioSource = GetComponentInChildren<AudioSource>();
        }
    }

    private void PlayResultSfx(AudioClip clip, float delay)
    {
        if (clip == null) return;

        if (delay > 0f)
        {
            StartCoroutine(PlayResultSfxDelayed(clip, delay));
            return;
        }

        PlayResultSfxNow(clip);
    }

    private IEnumerator PlayResultSfxDelayed(AudioClip clip, float delay)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, delay));
        PlayResultSfxNow(clip);
    }

    private void PlayResultSfxNow(AudioClip clip)
    {
        if (clip == null) return;

        FindResultAudioSourceIfNeeded();

        if (resultAudioSource != null)
        {
            resultAudioSource.PlayOneShot(clip, resultSfxVolume);
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

    private int GetStartScore()
    {
        return currentSetting != null ? currentSetting.startScore : 12000;
    }

    private float GetScoreDecreasePerSecond()
    {
        return currentSetting != null ? currentSetting.scoreDecreasePerSecond : 36f;
    }

    private int GetCrystalBreakBonus()
    {
        return currentSetting != null ? currentSetting.crystalBreakBonus : 500;
    }

    private int GetDeathPenalty()
    {
        return currentSetting != null ? currentSetting.deathPenalty : 300;
    }

    private int GetSRankScore()
    {
        return currentSetting != null ? currentSetting.sRankScore : 10000;
    }

    private int GetARankScore()
    {
        return currentSetting != null ? currentSetting.aRankScore : 8000;
    }

    private int GetBRankScore()
    {
        return currentSetting != null ? currentSetting.bRankScore : 6000;
    }

    private int GetCRankScore()
    {
        return currentSetting != null ? currentSetting.cRankScore : 4000;
    }
}