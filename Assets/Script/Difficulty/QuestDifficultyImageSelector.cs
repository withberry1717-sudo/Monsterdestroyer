using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class QuestDifficultyImageSelector : MonoBehaviour
{
    public enum Difficulty
    {
        Easy,
        Normal,
        Hard
    }

    public static Difficulty SelectedDifficulty = Difficulty.Normal;

    public const string DifficultySaveKey = "SelectedDifficulty";

    [System.Serializable]
    public class DifficultyImageData
    {
        [Header("難易度")]
        public Difficulty difficulty;

        [Header("この難易度で表示するクエストペーパー画像")]
        public Sprite questPaperSprite;

        [Header("この難易度で表示する依頼文画像")]
        public Sprite questTextSprite;

        [Header("背景パネル色")]
        public Color panelColor = Color.white;
    }

    [Header("切り替えるImage本体")]
    [Tooltip("QuestPanel自身のImage。パネル色を変えたい場合に入れる")]
    [SerializeField] private Image questPanelImage;

    [Tooltip("クエストペーパーを表示しているImageオブジェクトを入れる")]
    [SerializeField] private Image questPaperImage;

    [Tooltip("依頼文を表示しているImageオブジェクトを入れる")]
    [SerializeField] private Image questTextImage;

    [Header("矢印ボタン")]
    [Tooltip("左矢印ボタン。OnClickは手動設定しなくてOK")]
    [SerializeField] private Button leftArrowButton;

    [Tooltip("右矢印ボタン。OnClickは手動設定しなくてOK")]
    [SerializeField] private Button rightArrowButton;

    [Header("難易度ごとの画像")]
    [SerializeField] private DifficultyImageData[] difficulties;

    [Header("切り替え演出")]
    [SerializeField] private bool usePopAnimation = true;
    [SerializeField] private float popScale = 1.05f;
    [SerializeField] private float popSpeed = 14f;

    [Header("開始設定")]
    [SerializeField] private Difficulty defaultDifficulty = Difficulty.Normal;

    [Tooltip("前回選んだ難易度を読み込む。OFFなら毎回Default Difficultyから開始")]
    [SerializeField] private bool loadSavedDifficultyOnStart = true;

    private int currentIndex = 0;
    private Vector3 paperOriginalScale;
    private Vector3 textOriginalScale;
    private Coroutine popCoroutine;

    private void Awake()
    {
        if (questPaperImage != null)
        {
            paperOriginalScale = questPaperImage.transform.localScale;
        }

        if (questTextImage != null)
        {
            textOriginalScale = questTextImage.transform.localScale;
        }
    }

    private void Start()
    {
        RegisterButtonEvents();

        Difficulty startDifficulty = defaultDifficulty;

        if (loadSavedDifficultyOnStart)
        {
            startDifficulty = LoadSavedDifficulty(defaultDifficulty);
        }

        SelectedDifficulty = startDifficulty;
        currentIndex = FindIndexByDifficulty(startDifficulty);

        ApplyDifficulty();
    }

    private void OnDestroy()
    {
        UnregisterButtonEvents();
    }

    private void RegisterButtonEvents()
    {
        if (leftArrowButton != null)
        {
            leftArrowButton.onClick.RemoveListener(SelectPrevious);
            leftArrowButton.onClick.AddListener(SelectPrevious);
        }

        if (rightArrowButton != null)
        {
            rightArrowButton.onClick.RemoveListener(SelectNext);
            rightArrowButton.onClick.AddListener(SelectNext);
        }
    }

    private void UnregisterButtonEvents()
    {
        if (leftArrowButton != null)
        {
            leftArrowButton.onClick.RemoveListener(SelectPrevious);
        }

        if (rightArrowButton != null)
        {
            rightArrowButton.onClick.RemoveListener(SelectNext);
        }
    }

    public void SelectPrevious()
    {
        if (!HasDifficultyData()) return;

        currentIndex--;

        if (currentIndex < 0)
        {
            currentIndex = difficulties.Length - 1;
        }

        ApplyDifficulty();
    }

    public void SelectNext()
    {
        if (!HasDifficultyData()) return;

        currentIndex++;

        if (currentIndex >= difficulties.Length)
        {
            currentIndex = 0;
        }

        ApplyDifficulty();
    }

    private void ApplyDifficulty()
    {
        if (!HasDifficultyData()) return;

        DifficultyImageData data = difficulties[currentIndex];

        SelectedDifficulty = data.difficulty;
        SaveDifficulty(SelectedDifficulty);

        if (questPaperImage != null)
        {
            questPaperImage.sprite = data.questPaperSprite;
            questPaperImage.preserveAspect = true;
            questPaperImage.enabled = data.questPaperSprite != null;
        }

        if (questTextImage != null)
        {
            questTextImage.sprite = data.questTextSprite;
            questTextImage.preserveAspect = true;
            questTextImage.enabled = data.questTextSprite != null;
        }

        if (questPanelImage != null)
        {
            questPanelImage.color = data.panelColor;
        }

        if (usePopAnimation)
        {
            PlayPopAnimation();
        }

        Debug.Log("Difficulty Selected: " + SelectedDifficulty);
    }

    private void SaveDifficulty(Difficulty difficulty)
    {
        PlayerPrefs.SetInt(DifficultySaveKey, (int)difficulty);
        PlayerPrefs.Save();
    }

    public static Difficulty LoadSavedDifficulty(Difficulty fallback = Difficulty.Normal)
    {
        int savedValue = PlayerPrefs.GetInt(DifficultySaveKey, (int)fallback);

        if (System.Enum.IsDefined(typeof(Difficulty), savedValue))
        {
            return (Difficulty)savedValue;
        }

        return fallback;
    }

    private void PlayPopAnimation()
    {
        if (popCoroutine != null)
        {
            StopCoroutine(popCoroutine);
        }

        popCoroutine = StartCoroutine(PopRoutine());
    }

    private IEnumerator PopRoutine()
    {
        float t = 0f;

        Vector3 paperStartScale = paperOriginalScale * popScale;
        Vector3 textStartScale = textOriginalScale * popScale;

        if (questPaperImage != null)
        {
            questPaperImage.transform.localScale = paperStartScale;
        }

        if (questTextImage != null)
        {
            questTextImage.transform.localScale = textStartScale;
        }

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * popSpeed;
            float easedT = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);

            if (questPaperImage != null)
            {
                questPaperImage.transform.localScale =
                    Vector3.Lerp(paperStartScale, paperOriginalScale, easedT);
            }

            if (questTextImage != null)
            {
                questTextImage.transform.localScale =
                    Vector3.Lerp(textStartScale, textOriginalScale, easedT);
            }

            yield return null;
        }

        if (questPaperImage != null)
        {
            questPaperImage.transform.localScale = paperOriginalScale;
        }

        if (questTextImage != null)
        {
            questTextImage.transform.localScale = textOriginalScale;
        }

        popCoroutine = null;
    }

    private bool HasDifficultyData()
    {
        return difficulties != null && difficulties.Length > 0;
    }

    private int FindIndexByDifficulty(Difficulty difficulty)
    {
        if (!HasDifficultyData())
        {
            return 0;
        }

        for (int i = 0; i < difficulties.Length; i++)
        {
            if (difficulties[i].difficulty == difficulty)
            {
                return i;
            }
        }

        return 0;
    }
}