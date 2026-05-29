using UnityEngine;

public class TailSeverController : MonoBehaviour
{
    [Header("ドラゴンメッシュ切り替え")]
    [Tooltip("破壊前の通常ドラゴンメッシュ。尻尾と結晶がある方を入れる")]
    [SerializeField] private GameObject normalDragonMeshObject;

    [Tooltip("破壊後のドラゴンメッシュ。尻尾が切断された本体を入れる。最初は非表示推奨")]
    [SerializeField] private GameObject brokenDragonMeshObject;

    [Header("吹っ飛ぶ尻尾")]
    [Tooltip("切断されて吹っ飛ぶ尻尾Prefab。RigidbodyとColliderを付けておく")]
    [SerializeField] private GameObject severedTailPrefab;

    [Tooltip("尻尾を生成する位置。尻尾の根元〜切断位置あたりに空オブジェクトを置く")]
    [SerializeField] private Transform severedTailSpawnPoint;

    [Tooltip("尻尾を吹っ飛ばす方向の基準。未設定ならこのオブジェクトの向きを使う")]
    [SerializeField] private Transform severedTailForceDirection;

    [Tooltip("尻尾を前後方向に吹っ飛ばす力")]
    [SerializeField] private float tailFlyForce = 8f;

    [Tooltip("尻尾を上方向に浮かせる力")]
    [SerializeField] private float tailUpForce = 3f;

    [Tooltip("尻尾を回転させる力")]
    [SerializeField] private float tailTorqueForce = 8f;

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
    [Tooltip("切断SE再生用AudioSource。未設定なら親から探す")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("尻尾切断時のSE")]
    [SerializeField] private AudioClip severSfx;

    [Range(0f, 1f)]
    [SerializeField] private float severSfxVolume = 1f;

    private bool hasSevered = false;

    private void Awake()
    {
        if (brokenDragonMeshObject != null)
        {
            brokenDragonMeshObject.SetActive(false);
        }

        if (normalDragonMeshObject != null)
        {
            normalDragonMeshObject.SetActive(true);
        }

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

        SwitchDragonMesh();
        SpawnSeveredTail();
        DisableOldHitboxes();
        PlaySeverParticle();
        PlaySeverSfx();
    }

    private void SwitchDragonMesh()
    {
        if (normalDragonMeshObject != null)
        {
            normalDragonMeshObject.SetActive(false);
        }

        if (brokenDragonMeshObject != null)
        {
            brokenDragonMeshObject.SetActive(true);
        }
    }

    private void SpawnSeveredTail()
    {
        if (severedTailPrefab == null) return;

        Vector3 spawnPosition = transform.position;
        Quaternion spawnRotation = transform.rotation;

        if (severedTailSpawnPoint != null)
        {
            spawnPosition = severedTailSpawnPoint.position;
            spawnRotation = severedTailSpawnPoint.rotation;
        }

        GameObject tail = Instantiate(severedTailPrefab, spawnPosition, spawnRotation);

        Rigidbody rb = tail.GetComponent<Rigidbody>();

        if (rb != null)
        {
            Transform directionTransform = severedTailForceDirection != null
                ? severedTailForceDirection
                : transform;

            Vector3 flyDirection = -directionTransform.forward + Vector3.up * 0.35f;
            Vector3 force = flyDirection.normalized * tailFlyForce + Vector3.up * tailUpForce;

            rb.AddForce(force, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * tailTorqueForce, ForceMode.Impulse);
        }

        if (severedTailDestroyDelay > 0f)
        {
            Destroy(tail, severedTailDestroyDelay);
        }
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
}