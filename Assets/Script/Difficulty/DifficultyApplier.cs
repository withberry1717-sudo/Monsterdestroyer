using UnityEngine;

public class DifficultyApplier : MonoBehaviour
{
    [Header("Dragon References")]
    [SerializeField] private DragonHP dragonHP;

    [Tooltip("ドラゴンAI。行動間隔・移動速度・アニメ速度・HP50%ダウン時間を難易度で変えるために使う")]
    [SerializeField] private DragonAI dragonAI;

    [Tooltip("ドラゴン全体の親Transform。Easyで小さくしたいオブジェクトを入れる")]
    [SerializeField] private Transform dragonRoot;

    [Header("Dragon Attack Hitboxes")]
    [Tooltip("空ならDragon Root以下からDragonAttackHitboxを自動取得します。手動で指定したい場合はここに全部入れてください。")]
    [SerializeField] private DragonAttackHitbox[] dragonAttackHitboxes;

    [Tooltip("ONならDragon Attack Hitboxesが空の時にDragon Root以下から自動取得します。")]
    [SerializeField] private bool autoFindDragonAttackHitboxes = true;

    [Header("Tail Break Reaction")]
    [Tooltip("ONなら尻尾切断時はDownではなくBigHitを再生する")]
    [SerializeField] private bool useBigHitOnTailBreak = true;

    [Tooltip("尻尾切断時に再生するAnimatorのState名。Animator側と完全一致させる")]
    [SerializeField] private string tailBreakBigHitAnimName = "Big hit";

    [Header("Easy")]
    [SerializeField] private float easyDragonHpMultiplier = 0.75f;
    [SerializeField] private float easyCrystalHpMultiplier = 0.75f;
    [SerializeField] private float easyActionIntervalMultiplier = 1.15f;
    [SerializeField] private float easyMoveSpeedMultiplier = 0.9f;
    [SerializeField] private float easyAnimationSpeedMultiplier = 0.9f;
    [SerializeField] private float easyDragonDamageMultiplier = 0.75f;
    [SerializeField] private float easyDragonScaleMultiplier = 0.9f;
    [Tooltip("HP50%で第2形態に入る時のダウン時間")]
    [SerializeField] private float easyHalfHPDownDuration = 3.8f;
    [Tooltip("尻尾切断BigHitの硬直時間")]
    [SerializeField] private float easyTailBreakBigHitDuration = 1.0f;

    [Header("Normal")]
    [SerializeField] private float normalDragonHpMultiplier = 0.9f;
    [SerializeField] private float normalCrystalHpMultiplier = 0.9f;
    [SerializeField] private float normalActionIntervalMultiplier = 1.0f;
    [SerializeField] private float normalMoveSpeedMultiplier = 1.0f;
    [SerializeField] private float normalAnimationSpeedMultiplier = 1.0f;
    [SerializeField] private float normalDragonDamageMultiplier = 1.0f;
    [SerializeField] private float normalDragonScaleMultiplier = 1.0f;
    [Tooltip("HP50%で第2形態に入る時のダウン時間")]
    [SerializeField] private float normalHalfHPDownDuration = 5.0f;
    [Tooltip("尻尾切断BigHitの硬直時間")]
    [SerializeField] private float normalTailBreakBigHitDuration = 1.2f;

    [Header("Hard")]
    [SerializeField] private float hardDragonHpMultiplier = 1.25f;
    [SerializeField] private float hardCrystalHpMultiplier = 1.15f;
    [SerializeField] private float hardActionIntervalMultiplier = 0.85f;
    [SerializeField] private float hardMoveSpeedMultiplier = 1.1f;
    [SerializeField] private float hardAnimationSpeedMultiplier = 1.1f;
    [SerializeField] private float hardDragonDamageMultiplier = 1.25f;
    [SerializeField] private float hardDragonScaleMultiplier = 1.0f;
    [Tooltip("HP50%で第2形態に入る時のダウン時間")]
    [SerializeField] private float hardHalfHPDownDuration = 4.0f;
    [Tooltip("尻尾切断BigHitの硬直時間")]
    [SerializeField] private float hardTailBreakBigHitDuration = 1.0f;

    [Header("Hard Special Tuning")]
    [Tooltip("Hard限定。ドラゴンが攻撃行動を選んだあと、実際に攻撃を始める前に入れる追加ディレイです。0なら追加なし。")]
    [SerializeField] private float hardAttackStartDelay = 0.18f;

    [Tooltip("Hard限定。ONなら突進前の溜め時間をHard専用値で上書きします。")]
    [SerializeField] private bool hardOverrideChargeTellTime = true;

    [Tooltip("Hard限定の突進溜め時間です。短くすると突進の予兆が短くなります。")]
    [SerializeField] private float hardChargeTellTime = 0.35f;

    [Tooltip("Hard限定。ONなら突進直前の停止時間をHard専用値で上書きします。")]
    [SerializeField] private bool hardOverrideChargeReadyPause = true;

    [Tooltip("Hard限定の突進直前停止時間です。短くすると溜め後すぐ突進します。")]
    [SerializeField] private float hardChargeReadyPause = 0.05f;

    [Header("Hard Only Charge Homing")]
    [Tooltip("Hard限定。ONなら突進中に少しだけプレイヤー方向へ曲がります。")]
    [SerializeField] private bool hardUseChargeHoming = true;

    [Tooltip("Hard限定。突進追尾の強さです。0.25〜0.45くらいが自然です。")]
    [Range(0f, 1f)]
    [SerializeField] private float hardChargeHomingStrength = 0.35f;

    [Tooltip("Hard限定。突進中に1秒で曲がれる最大角度です。")]
    [SerializeField] private float hardChargeHomingMaxTurnSpeed = 90f;

    [Header("Hard Only Tail Pre-Hit Delay")]
    [Tooltip("Hard限定。ONならTailSlam/TailSwipe一段目の判定直前に短いディレイを入れ、その間もプレイヤーを追います。")]
    [SerializeField] private bool hardUseTailPreHitDelay = true;

    [Tooltip("Hard限定。TailSlamの当たり判定直前ディレイです。")]
    [SerializeField] private float hardTailSlamPreHitDelay = 0.12f;

    [Tooltip("Hard限定。TailSwipe一段目の当たり判定直前ディレイです。")]
    [SerializeField] private float hardTailSwipeFirstPreHitDelay = 0.12f;

    [Tooltip("Hard限定。尻尾直前ディレイ中の追尾旋回速度です。")]
    [SerializeField] private float hardTailPreHitTurnSpeed = 22f;

    [Tooltip("ON推奨。尻尾直前ディレイ中はアニメを一時停止します。")]
    [SerializeField] private bool hardPauseAnimatorDuringTailDelay = true;

    [Header("Debug")]
    [SerializeField] private bool logAppliedDamageMultiplier = true;

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
                ApplyDragonAI(
                    easyActionIntervalMultiplier,
                    easyMoveSpeedMultiplier,
                    easyAnimationSpeedMultiplier,
                    easyHalfHPDownDuration,
                    easyTailBreakBigHitDuration,
                    0f,
                    false,
                    0f,
                    false,
                    0f,
                    false,
                    0f,
                    0f,
                    false,
                    0f,
                    0f,
                    0f,
                    false
                );
                ApplyDragonDamage(easyDragonDamageMultiplier);
                ApplyDragonScale(easyDragonScaleMultiplier);
                break;

            case QuestDifficultyImageSelector.Difficulty.Normal:
                ApplyDragonHp(normalDragonHpMultiplier, normalCrystalHpMultiplier);
                ApplyDragonAI(
                    normalActionIntervalMultiplier,
                    normalMoveSpeedMultiplier,
                    normalAnimationSpeedMultiplier,
                    normalHalfHPDownDuration,
                    normalTailBreakBigHitDuration,
                    0f,
                    false,
                    0f,
                    false,
                    0f,
                    false,
                    0f,
                    0f,
                    false,
                    0f,
                    0f,
                    0f,
                    false
                );
                ApplyDragonDamage(normalDragonDamageMultiplier);
                ApplyDragonScale(normalDragonScaleMultiplier);
                break;

            case QuestDifficultyImageSelector.Difficulty.Hard:
                ApplyDragonHp(hardDragonHpMultiplier, hardCrystalHpMultiplier);
                ApplyDragonAI(
                    hardActionIntervalMultiplier,
                    hardMoveSpeedMultiplier,
                    hardAnimationSpeedMultiplier,
                    hardHalfHPDownDuration,
                    hardTailBreakBigHitDuration,
                    hardAttackStartDelay,
                    hardOverrideChargeTellTime,
                    hardChargeTellTime,
                    hardOverrideChargeReadyPause,
                    hardChargeReadyPause,
                    hardUseChargeHoming,
                    hardChargeHomingStrength,
                    hardChargeHomingMaxTurnSpeed,
                    hardUseTailPreHitDelay,
                    hardTailSlamPreHitDelay,
                    hardTailSwipeFirstPreHitDelay,
                    hardTailPreHitTurnSpeed,
                    hardPauseAnimatorDuringTailDelay
                );
                ApplyDragonDamage(hardDragonDamageMultiplier);
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

    private void ApplyDragonAI(
        float actionIntervalMultiplier,
        float moveSpeedMultiplier,
        float animationSpeedMultiplier,
        float halfHPDownDuration,
        float tailBreakBigHitDuration,
        float attackStartDelay,
        bool overrideChargeTellTime,
        float chargeTellTime,
        bool overrideChargeReadyPause,
        float chargeReadyPause,
        bool enableChargeHoming,
        float chargeHomingStrength,
        float chargeHomingMaxTurnSpeed,
        bool enableTailPreHitDelay,
        float tailSlamPreHitDelay,
        float tailSwipeFirstPreHitDelay,
        float tailPreHitTurnSpeed,
        bool pauseAnimatorDuringTailDelay
    )
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

        dragonAI.SetDifficultyHalfHPDownDuration(halfHPDownDuration);

        dragonAI.SetTailBreakBigHitSettings(
            useBigHitOnTailBreak,
            tailBreakBigHitAnimName,
            tailBreakBigHitDuration
        );

        dragonAI.SetDifficultySpecialAttackTiming(
            attackStartDelay,
            overrideChargeTellTime,
            chargeTellTime,
            overrideChargeReadyPause,
            chargeReadyPause
        );

        dragonAI.SetDifficultyHardOnlyExtraTuning(
            enableChargeHoming,
            chargeHomingStrength,
            chargeHomingMaxTurnSpeed,
            enableTailPreHitDelay,
            tailSlamPreHitDelay,
            tailSwipeFirstPreHitDelay,
            tailPreHitTurnSpeed,
            pauseAnimatorDuringTailDelay
        );
    }

    private void ApplyDragonDamage(float damageMultiplier)
    {
        DragonAttackHitbox[] hitboxes = GetDragonAttackHitboxes();

        if (hitboxes == null || hitboxes.Length == 0)
        {
            Debug.LogWarning("DifficultyApplier: DragonAttackHitbox が見つかりません。Dragon RootかDragon Attack Hitboxesを確認してください。");
            return;
        }

        foreach (DragonAttackHitbox hitbox in hitboxes)
        {
            if (hitbox == null) continue;
            hitbox.SetDifficultyDamageMultiplier(damageMultiplier);
        }

        if (logAppliedDamageMultiplier)
        {
            Debug.Log($"DifficultyApplier: DragonAttackHitbox {hitboxes.Length}個にダメージ倍率 x{damageMultiplier} を適用しました。");
        }
    }

    private DragonAttackHitbox[] GetDragonAttackHitboxes()
    {
        if (dragonAttackHitboxes != null && dragonAttackHitboxes.Length > 0)
        {
            return dragonAttackHitboxes;
        }

        if (!autoFindDragonAttackHitboxes)
        {
            return dragonAttackHitboxes;
        }

        if (dragonRoot != null)
        {
            dragonAttackHitboxes = dragonRoot.GetComponentsInChildren<DragonAttackHitbox>(true);
        }
        else
        {
            dragonAttackHitboxes = FindObjectsByType<DragonAttackHitbox>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        return dragonAttackHitboxes;
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
