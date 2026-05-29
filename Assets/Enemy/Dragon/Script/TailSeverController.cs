using UnityEngine;

public class TailSeverController : MonoBehaviour
{
    [Header("ドラゴン本体メッシュ差し替え")]
    [Tooltip("Hierarchy上の通常ドラゴンのDragonMeshについているSkinned Mesh Rendererを入れる")]
    [SerializeField] private SkinnedMeshRenderer targetSkinnedMeshRenderer;

    [Tooltip("尻尾切断後のMesh。Project内のDragonMesh_Broken.fbxの中にあるDragon Meshを入れる")]
    [SerializeField] private Mesh brokenMesh;

    [Tooltip("切断後だけ別マテリアルにしたい場合に入れる。空なら現在のマテリアルをそのまま使う")]
    [SerializeField] private Material brokenMaterial;

    [Tooltip("切断時にSkinned Mesh RendererのBoundsを再計算します。表示が消える場合はON推奨")]
    [SerializeField] private bool recalculateBoundsOnSever = true;

    [Header("吹っ飛ぶ尻尾")]
    [Tooltip("切断されて吹っ飛ぶ尻尾Prefab。親にRigidbody、子か親にColliderを付けておく")]
    [SerializeField] private GameObject severedTailPrefab;

    [Tooltip("尻尾を生成する位置。尻尾ボーンの子に置くとアニメーションに追従します")]
    [SerializeField] private Transform severedTailSpawnPoint;

    [Tooltip("尻尾を吹っ飛ばす方向の基準。未設定ならSpawnPoint、それもなければこのオブジェクトの向きを使います")]
    [SerializeField] private Transform severedTailForceDirection;

    [Header("吹っ飛ぶ尻尾の見た目補正")]
    [Tooltip("尻尾Prefabを生成する位置の補正。SeveredTailSpawnPoint基準のローカル座標で調整します")]
    [SerializeField] private Vector3 severedTailLocalPositionOffset = Vector3.zero;

    [Tooltip("尻尾Prefabを生成する回転の補正。向きがズレる場合に調整します")]
    [SerializeField] private Vector3 severedTailLocalRotationOffset = Vector3.zero;

    [Tooltip("尻尾Prefabの大きさ補正。Prefabが小さい/大きい場合に調整します")]
    [SerializeField] private Vector3 severedTailScaleMultiplier = Vector3.one;

    [Header("吹っ飛び挙動")]
    [Tooltip("ONなら、吹っ飛ぶ方向はSevered Tail Force DirectionのForward方向を使います。OFFならBackward方向を使います")]
    [SerializeField] private bool useForceDirectionForward = true;

    [Tooltip("尻尾を横方向・前後方向に吹っ飛ばす力")]
    [SerializeField] private float tailFlyForce = 8f;

    [Tooltip("尻尾を上方向に浮かせる力")]
    [SerializeField] private float tailUpForce = 3f;

    [Tooltip("尻尾を回転させる力")]
    [SerializeField] private float tailTorqueForce = 8f;

    [Tooltip("生成直後にドラゴンの親子関係から外します。基本ON推奨")]
    [SerializeField] private bool detachSpawnedTailFromParent = true;

    [Tooltip("吹っ飛んだ尻尾を何秒後に消すか。0以下なら消さない")]
    [SerializeField] private float severedTailDestroyDelay = 12f;

    [Header("破壊後にOFFにする当たり判定")]
    [Tooltip("結晶Hurtboxや尻尾攻撃判定など、切断後に無効化したいCollider")]
    [SerializeField] private Collider[] collidersToDisable;

    [Tooltip("結晶用DragonHurtbox。切断後に無効化したいものを入れる")]
    [SerializeField] private DragonHurtbox[] hurtboxesToDisable;

    [Tooltip("切断後に非表示にしたいオブジェクト。結晶の見た目や尻尾攻撃判定Rootなど")]
    [SerializeField] private GameObject[] objectsToDisable;

    [Header("切断パーティクル")]
    [Tooltip("尻尾切断時に出すパーティクルPrefab")]
    [SerializeField] private GameObject severParticlePrefab;

    [Tooltip("パーティクルを出す位置。未設定ならSeveredTailSpawnPointを使う")]
    [SerializeField] private Transform severParticlePoint;

    [Tooltip("パーティクルを何秒後に消すか")]
    [SerializeField] private float particleDestroyDelay = 5f;

    [Header("切断SE")]
    [Tooltip("切断SE再生用AudioSource。未設定なら自分か親から探します")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("尻尾切断時のSE")]
    [SerializeField] private AudioClip severSfx;

    [Range(0f, 1f)]
    [SerializeField] private float severSfxVolume = 1f;

    private Mesh normalMesh;
    private Material[] normalMaterials;
    private bool hasSevered = false;

    private void Awake()
    {
        CacheNormalMeshAndMaterials();
        FindAudioSourceIfNeeded();
    }

    private void CacheNormalMeshAndMaterials()
    {
        if (targetSkinnedMeshRenderer == null)
        {
            return;
        }

        normalMesh = targetSkinnedMeshRenderer.sharedMesh;
        normalMaterials = targetSkinnedMeshRenderer.sharedMaterials;
    }

    private void FindAudioSourceIfNeeded()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponentInParent<AudioSource>();
        }
    }

    public void SeverTail()
    {
        if (hasSevered) return;
        hasSevered = true;

        SwitchToBrokenMesh();
        SpawnSeveredTail();
        DisableOldHitboxes();
        PlaySeverParticle();
        PlaySeverSfx();
    }

    private void SwitchToBrokenMesh()
    {
        if (targetSkinnedMeshRenderer == null)
        {
            Debug.LogWarning($"{name}: Target Skinned Mesh Renderer が設定されていません。");
            return;
        }

        if (brokenMesh == null)
        {
            Debug.LogWarning($"{name}: Broken Mesh が設定されていません。");
            return;
        }

        targetSkinnedMeshRenderer.sharedMesh = brokenMesh;

        if (brokenMaterial != null)
        {
            targetSkinnedMeshRenderer.sharedMaterial = brokenMaterial;
        }

        if (recalculateBoundsOnSever)
        {
            targetSkinnedMeshRenderer.localBounds = brokenMesh.bounds;
        }

        Debug.Log("尻尾切断後メッシュに差し替えました。");
    }

    private void SpawnSeveredTail()
    {
        if (severedTailPrefab == null)
        {
            return;
        }

        Vector3 spawnPosition = transform.position;
        Quaternion spawnRotation = transform.rotation;

        if (severedTailSpawnPoint != null)
        {
            spawnPosition = severedTailSpawnPoint.TransformPoint(severedTailLocalPositionOffset);
            spawnRotation = severedTailSpawnPoint.rotation * Quaternion.Euler(severedTailLocalRotationOffset);
        }

        GameObject tail = Instantiate(severedTailPrefab, spawnPosition, spawnRotation);

        if (detachSpawnedTailFromParent)
        {
            tail.transform.SetParent(null, true);
        }

        tail.transform.localScale = Vector3.Scale(tail.transform.localScale, severedTailScaleMultiplier);

        Rigidbody rb = tail.GetComponent<Rigidbody>();

        if (rb != null)
        {
            Transform directionTransform = GetForceDirectionTransform();

            Vector3 horizontalDirection = useForceDirectionForward
                ? directionTransform.forward
                : -directionTransform.forward;

            Vector3 force = horizontalDirection.normalized * tailFlyForce + Vector3.up * tailUpForce;

            rb.AddForce(force, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * tailTorqueForce, ForceMode.Impulse);
        }
        else
        {
            Debug.LogWarning($"{tail.name}: Rigidbody が付いていないため、尻尾は吹っ飛びません。");
        }

        if (severedTailDestroyDelay > 0f)
        {
            Destroy(tail, severedTailDestroyDelay);
        }
    }

    private Transform GetForceDirectionTransform()
    {
        if (severedTailForceDirection != null)
        {
            return severedTailForceDirection;
        }

        if (severedTailSpawnPoint != null)
        {
            return severedTailSpawnPoint;
        }

        return transform;
    }

    private void DisableOldHitboxes()
    {
        if (collidersToDisable != null)
        {
            foreach (Collider col in collidersToDisable)
            {
                if (col != null)
                {
                    col.enabled = false;
                }
            }
        }

        if (hurtboxesToDisable != null)
        {
            foreach (DragonHurtbox hurtbox in hurtboxesToDisable)
            {
                if (hurtbox != null)
                {
                    hurtbox.enabled = false;
                }
            }
        }

        if (objectsToDisable != null)
        {
            foreach (GameObject obj in objectsToDisable)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
        }
    }

    private void PlaySeverParticle()
    {
        if (severParticlePrefab == null) return;

        Vector3 spawnPosition = transform.position;
        Quaternion spawnRotation = Quaternion.identity;

        if (severParticlePoint != null)
        {
            spawnPosition = severParticlePoint.position;
            spawnRotation = severParticlePoint.rotation;
        }
        else if (severedTailSpawnPoint != null)
        {
            spawnPosition = severedTailSpawnPoint.position;
            spawnRotation = severedTailSpawnPoint.rotation;
        }

        GameObject particle = Instantiate(severParticlePrefab, spawnPosition, spawnRotation);

        if (particleDestroyDelay > 0f)
        {
            Destroy(particle, particleDestroyDelay);
        }
    }

    private void PlaySeverSfx()
    {
        if (audioSource == null) return;
        if (severSfx == null) return;

        audioSource.PlayOneShot(severSfx, severSfxVolume);
    }

    [ContextMenu("Debug Sever Tail")]
    private void DebugSeverTail()
    {
        SeverTail();
    }

    [ContextMenu("Restore Normal Mesh")]
    public void RestoreNormalMesh()
    {
        if (targetSkinnedMeshRenderer == null) return;
        if (normalMesh == null) return;

        targetSkinnedMeshRenderer.sharedMesh = normalMesh;

        if (normalMaterials != null && normalMaterials.Length > 0)
        {
            targetSkinnedMeshRenderer.sharedMaterials = normalMaterials;
        }

        hasSevered = false;
    }
}