using UnityEngine;

public class DifficultyApplier : MonoBehaviour
{
    [Header("Dragon References")]
    [SerializeField] private DragonHP dragonHP;

    [Tooltip("ドラゴンAI。行動間隔・移動速度・アニメ速度を難易度で変えるために使う")]
    [SerializeField] private DragonAI dragonAI;

    [Tooltip("ドラゴン全体の親Transform。Easyで小さくしたいオブジェクトを入れる")]
    [SerializeField] private Transform dragonRoot;

    [Header("Easy")]
    [SerializeField] private float easyDragonHpMultiplier = 0.75f;
    [SerializeField] private float easyCrystalHpMultiplier = 0.75f;
    [SerializeField] private float easyActionIntervalMultiplier = 1.15f;
    [SerializeField] private float easyMoveSpeedMultiplier = 0.9f;
    [SerializeField] private float easyAnimationSpeedMultiplier = 0.9f;
    [SerializeField] private float easyDragonScaleMultiplier = 0.9f;

    [Header("Normal")]
    [SerializeField] private float normalDragonHpMultiplier = 0.9f;
    [SerializeField] private float normalCrystalHpMultiplier = 0.9f;
    [SerializeField] private float normalActionIntervalMultiplier = 1.0f;
    [SerializeField] private float normalMoveSpeedMultiplier = 1.0f;
    [SerializeField] private float normalAnimationSpeedMultiplier = 1.0f;
    [SerializeField] private float normalDragonScaleMultiplier = 1.0f;

    [Header("Hard")]
    [SerializeField] private float hardDragonHpMultiplier = 1.25f;
    [SerializeField] private float hardCrystalHpMultiplier = 1.15f;
    [SerializeField] private float hardActionIntervalMultiplier = 0.85f;
    [SerializeField] private float hardMoveSpeedMultiplier = 1.1f;
    [SerializeField] private float hardAnimationSpeedMultiplier = 1.1f;
    [SerializeField] private float hardDragonScaleMultiplier = 1.0f;

    private Vector3 originalDragonScale = Vector3.one;

    private void Awake()
    {
        if (dragonRoot != null)
        {
            originalDragonScale = dragonRoot.localScale;
        }

        ApplyDifficulty();
    }

    private void ApplyDifficulty()
    {
        QuestDifficultyImageSelector.Difficulty difficulty =
            QuestDifficultyImageSelector.LoadSavedDifficulty();

        Debug.Log("Battle Difficulty: " + difficulty);

        switch (difficulty)
        {
            case QuestDifficultyImageSelector.Difficulty.Easy:
                ApplyDragonHp(easyDragonHpMultiplier, easyCrystalHpMultiplier);
                ApplyDragonAI(easyActionIntervalMultiplier, easyMoveSpeedMultiplier, easyAnimationSpeedMultiplier);
                ApplyDragonScale(easyDragonScaleMultiplier);
                break;

            case QuestDifficultyImageSelector.Difficulty.Normal:
                ApplyDragonHp(normalDragonHpMultiplier, normalCrystalHpMultiplier);
                ApplyDragonAI(normalActionIntervalMultiplier, normalMoveSpeedMultiplier, normalAnimationSpeedMultiplier);
                ApplyDragonScale(normalDragonScaleMultiplier);
                break;

            case QuestDifficultyImageSelector.Difficulty.Hard:
                ApplyDragonHp(hardDragonHpMultiplier, hardCrystalHpMultiplier);
                ApplyDragonAI(hardActionIntervalMultiplier, hardMoveSpeedMultiplier, hardAnimationSpeedMultiplier);
                ApplyDragonScale(hardDragonScaleMultiplier);
                break;
        }
    }

    private void ApplyDragonHp(float dragonHpMultiplier, float crystalHpMultiplier)
    {
        if (dragonHP == null)
        {
            Debug.LogWarning("DifficultyApplier: DragonHP が入っていません");
            return;
        }

        dragonHP.maxHP *= dragonHpMultiplier;
        dragonHP.currentHP = dragonHP.maxHP;

        dragonHP.maxTailCrystalHP *= crystalHpMultiplier;
        dragonHP.currentTailCrystalHP = dragonHP.maxTailCrystalHP;
    }

    private void ApplyDragonAI(float actionIntervalMultiplier, float moveSpeedMultiplier, float animationSpeedMultiplier)
    {
        if (dragonAI == null)
        {
            Debug.LogWarning("DifficultyApplier: DragonAI が入っていません");
            return;
        }

        dragonAI.SetDifficultyMultipliers(
            actionIntervalMultiplier,
            moveSpeedMultiplier,
            animationSpeedMultiplier
        );
    }

    private void ApplyDragonScale(float scaleMultiplier)
    {
        if (dragonRoot == null)
        {
            Debug.LogWarning("DifficultyApplier: Dragon Root が入っていません");
            return;
        }

        dragonRoot.localScale = originalDragonScale * scaleMultiplier;
    }
}
