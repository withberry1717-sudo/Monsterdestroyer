using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestDifficultySelector : MonoBehaviour
{
    public enum Difficulty
    {
        Easy,
        Normal,
        Hard
    }

    public static Difficulty SelectedDifficulty = Difficulty.Normal;

    [System.Serializable]
    public class DifficultyData
    {
        public Difficulty difficulty;

        [Header("表示名")]
        public string displayName;

        [TextArea(2, 5)]
        public string description;

        [Header("パネル色")]
        public Color panelColor = Color.white;

        [Header("文字色")]
        public Color textColor = Color.white;
    }

    [Header("UI References")]
    [SerializeField] private Image questPanelImage;
    [SerializeField] private TextMeshProUGUI difficultyTitleText;
    [SerializeField] private TextMeshProUGUI difficultyDescriptionText;
    [SerializeField] private Button leftArrowButton;
    [SerializeField] private Button rightArrowButton;

    [Header("Difficulty Settings")]
    [SerializeField]
    private DifficultyData[] difficulties =
    {
        new DifficultyData
        {
            difficulty = Difficulty.Easy,
            displayName = "EASY",
            description = "アクションに慣れていない人向け。\n敵の攻撃が少しゆるく、クリアしやすいモード。",
            panelColor = new Color(0.25f, 0.55f, 0.35f, 0.85f),
            textColor = Color.white
        },
        new DifficultyData
        {
            difficulty = Difficulty.Normal,
            displayName = "NORMAL",
            description = "標準難易度。\nこのゲームの基本となるバランス。",
            panelColor = new Color(0.25f, 0.35f, 0.65f, 0.85f),
            textColor = Color.white
        },
        new DifficultyData
        {
            difficulty = Difficulty.Hard,
            displayName = "HARD",
            description = "歯ごたえのある高難易度。\n敵の攻撃が激しく、回避と攻撃判断が重要。",
            panelColor = new Color(0.65f, 0.20f, 0.20f, 0.85f),
            textColor = Color.white
        }
    };

    [Header("Animation")]
    [SerializeField] private bool useScaleAnimation = true;
    [SerializeField] private float popScale = 1.08f;
    [SerializeField] private float popSpeed = 12f;

    private int currentIndex = 1;
    private Vector3 originalTitleScale;
    private Coroutine popCoroutine;

    private void Awake()
    {
        if (difficultyTitleText != null)
        {
            originalTitleScale = difficultyTitleText.transform.localScale;
        }
    }

    private void Start()
    {
        if (leftArrowButton != null)
        {
            leftArrowButton.onClick.AddListener(SelectPrevious);
        }

        if (rightArrowButton != null)
        {
            rightArrowButton.onClick.AddListener(SelectNext);
        }

        currentIndex = FindIndexByDifficulty(SelectedDifficulty);
        ApplyDifficulty();
    }

    private void OnDestroy()
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
        currentIndex--;

        if (currentIndex < 0)
        {
            currentIndex = difficulties.Length - 1;
        }

        ApplyDifficulty();
    }

    public void SelectNext()
    {
        currentIndex++;

        if (currentIndex >= difficulties.Length)
        {
            currentIndex = 0;
        }

        ApplyDifficulty();
    }

    private void ApplyDifficulty()
    {
        if (difficulties == null || difficulties.Length == 0)
        {
            return;
        }

        DifficultyData data = difficulties[currentIndex];

        SelectedDifficulty = data.difficulty;

        if (difficultyTitleText != null)
        {
            difficultyTitleText.text = data.displayName;
            difficultyTitleText.color = data.textColor;
        }

        if (difficultyDescriptionText != null)
        {
            difficultyDescriptionText.text = data.description;
            difficultyDescriptionText.color = data.textColor;
        }

        if (questPanelImage != null)
        {
            questPanelImage.color = data.panelColor;
        }

        if (useScaleAnimation && difficultyTitleText != null)
        {
            if (popCoroutine != null)
            {
                StopCoroutine(popCoroutine);
            }

            popCoroutine = StartCoroutine(PopTitleRoutine());
        }
    }

    private System.Collections.IEnumerator PopTitleRoutine()
    {
        Transform titleTransform = difficultyTitleText.transform;

        Vector3 startScale = originalTitleScale * popScale;
        Vector3 endScale = originalTitleScale;

        titleTransform.localScale = startScale;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * popSpeed;
            titleTransform.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }

        titleTransform.localScale = endScale;
        popCoroutine = null;
    }

    private int FindIndexByDifficulty(Difficulty difficulty)
    {
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