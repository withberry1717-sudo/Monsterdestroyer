using UnityEngine;
using UnityEngine.UI;

public class ClearPanelDifficultyVisual : MonoBehaviour
{
    [System.Serializable]
    public class DifficultyVisualData
    {
        public QuestDifficultyImageSelector.Difficulty difficulty;

        [Header("左のクエスト紙")]
        public Sprite clearPaperSprite;

        [Header("右の結果シート背景（必要なら）")]
        public Sprite resultSheetSprite;

        [Header("右の結果シート色（必要なら）")]
        public Color resultSheetColor = Color.white;
    }

    [Header("対象UI")]
    [Tooltip("左側の紙画像。たぶん ClearPanel/QuestPaper")]
    [SerializeField] private Image clearPaperImage;

    [Tooltip("右側の結果シート背景。たぶん ClearPanel/Image。使わないなら空でOK")]
    [SerializeField] private Image resultSheetImage;

    [Header("難易度ごとの見た目")]
    [SerializeField] private DifficultyVisualData[] visuals;

    private void OnEnable()
    {
        ApplyDifficultyVisual();
    }

    public void ApplyDifficultyVisual()
    {
        QuestDifficultyImageSelector.Difficulty difficulty =
            QuestDifficultyImageSelector.LoadSavedDifficulty();

        DifficultyVisualData data = FindVisualData(difficulty);

        if (data == null)
        {
            Debug.LogWarning("ClearPanelDifficultyVisual: 該当する難易度データがありません");
            return;
        }

        if (clearPaperImage != null)
        {
            clearPaperImage.sprite = data.clearPaperSprite;
            clearPaperImage.preserveAspect = true;
            clearPaperImage.enabled = data.clearPaperSprite != null;
        }

        if (resultSheetImage != null)
        {
            if (data.resultSheetSprite != null)
            {
                resultSheetImage.sprite = data.resultSheetSprite;
                resultSheetImage.preserveAspect = true;
                resultSheetImage.enabled = true;
            }

            resultSheetImage.color = data.resultSheetColor;
        }

        Debug.Log("ClearPanel Visual Applied: " + difficulty);
    }

    private DifficultyVisualData FindVisualData(QuestDifficultyImageSelector.Difficulty difficulty)
    {
        if (visuals == null || visuals.Length == 0)
        {
            return null;
        }

        for (int i = 0; i < visuals.Length; i++)
        {
            if (visuals[i].difficulty == difficulty)
            {
                return visuals[i];
            }
        }

        return null;
    }
}