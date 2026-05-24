using UnityEngine;

public class DragonHurtbox : MonoBehaviour
{
    [Header("本体のHP管理スクリプトを登録")]
    public DragonHP dragonHP;

    [Header("ダメージ倍率設定")]
    public float damageMultiplier = 1.0f;

    [Header("クリスタル部位かどうか")]
    public bool isCrystalPart = false;

    public void OnHit(float baseDamage)
    {
        float finalDamage = baseDamage * damageMultiplier;

        if (dragonHP == null)
        {
            Debug.LogWarning("DragonHPがセットされていません！");
            return;
        }

        if (isCrystalPart)
        {
            dragonHP.TakeCrystalDamage(finalDamage);
        }

        dragonHP.TakeDamage(finalDamage);
    }
}