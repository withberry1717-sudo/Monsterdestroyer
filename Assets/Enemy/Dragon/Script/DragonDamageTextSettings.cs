using UnityEngine;

public class DragonDamageTextSettings : MonoBehaviour
{
    [Header("Damage Text Spawner")]
    [Tooltip("Scene上にあるDamageTextSpawnerを入れてください。Project内のPrefabではなく、Hierarchyに置いたものを入れます。")]
    public GameObject damageTextSpawner;

    [Tooltip("ダメージ数字を出す高さ補正です。大きくすると数字が上に出ます。")]
    public float defaultHeightOffset = 0.8f;
}