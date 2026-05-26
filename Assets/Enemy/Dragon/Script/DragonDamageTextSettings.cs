using UnityEngine;

public class DragonDamageTextSettings : MonoBehaviour
{
    [Header("Damage Text")]
    [Tooltip("Scene上にあるDamageTextSpawnerを入れる")]
    public DamageTextSpawner damageTextSpawner;

    [Tooltip("ダメージ数字を出す高さ。大きくすると上に出ます")]
    public float heightOffset = 1.0f;
}