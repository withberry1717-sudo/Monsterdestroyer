using UnityEngine;

public class DragonPhaseController : MonoBehaviour
{
    [Header("Phase State")]
    [Tooltip("HP50%以下の第2形態になっているか")]
    public bool isPhase2 = false;

    [Header("Phase 2 Basic Buff")]
    [Tooltip("第2形態の移動速度倍率")]
    public float phase2SpeedMultiplier = 1.15f;

    [Tooltip("第2形態の行動間隔倍率。小さいほど行動が速い")]
    public float phase2ActionIntervalMultiplier = 0.7f;

    [Tooltip("第2形態の突進速度倍率")]
    public float phase2ChargeSpeedMultiplier = 1.12f;

    [Header("Phase 2 Special")]
    [Tooltip("第2形態で二連突進を使う確率")]
    [Range(0f, 1f)]
    public float doubleChargeChance = 0.25f;

    [Tooltip("第2形態で連続攻撃を使う確率")]
    [Range(0f, 1f)]
    public float rapidComboChance = 0.30f;

    [Tooltip("連続攻撃中の攻撃間隔")]
    public float rapidComboInterval = 0.15f;

    [Tooltip("連続攻撃後の大きめの隙")]
    public float rapidComboRecovery = 1.2f;

    [Header("Phase 2 Openings")]
    [Tooltip("第2形態でも隙を作る確率")]
    [Range(0f, 1f)]
    public float phase2OpeningChance = 0.16f;

    [Tooltip("第2形態の隙の最短時間")]
    public float phase2OpeningMinTime = 0.9f;

    [Tooltip("第2形態の隙の最長時間")]
    public float phase2OpeningMaxTime = 1.8f;

    public void EnterPhase2()
    {
        if (isPhase2) return;

        isPhase2 = true;
        Debug.Log("DragonPhaseController: 第2形態に移行");
    }

    public float ApplySpeed(float baseSpeed)
    {
        return isPhase2 ? baseSpeed * phase2SpeedMultiplier : baseSpeed;
    }

    public float ApplyChargeSpeed(float baseSpeed)
    {
        return isPhase2 ? baseSpeed * phase2SpeedMultiplier * phase2ChargeSpeedMultiplier : baseSpeed;
    }

    public float ApplyActionInterval(float baseInterval)
    {
        return isPhase2 ? baseInterval * phase2ActionIntervalMultiplier : baseInterval;
    }

    public bool ShouldUseDoubleCharge()
    {
        return isPhase2 && Random.value < doubleChargeChance;
    }

    public bool ShouldUseRapidCombo()
    {
        return isPhase2 && Random.value < rapidComboChance;
    }

    public bool ShouldUsePhase2Opening()
    {
        return isPhase2 && Random.value < phase2OpeningChance;
    }

    public float GetPhase2OpeningTime()
    {
        return Random.Range(phase2OpeningMinTime, phase2OpeningMaxTime);
    }
}