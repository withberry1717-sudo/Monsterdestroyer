using UnityEngine;

public class DragonHurtbox : MonoBehaviour
{
    [Header("本体のHP管理スクリプトを登録")]
    public DragonCore dragonCore;

    [Header("ダメージ倍率設定")]
    public float damageMultiplier = 1.0f;

    [Header("部位設定")]
    public bool isCrystalPart = false; // これにチェックを入れるだけ！HPはCoreが持つ

    public void OnHit(float baseDamage)
    {
        float finalDamage = baseDamage * damageMultiplier;

        // もし自分がクリスタルの一部なら、Coreの「共有クリスタルHP」を減らすようにお願いする
        if (isCrystalPart)
        {
            dragonCore.TakeCrystalDamage(finalDamage);
        }

        // 本体にもダメージを送る
        if (dragonCore != null)
        {
            dragonCore.TakeDamage(finalDamage);
        }
    }
}