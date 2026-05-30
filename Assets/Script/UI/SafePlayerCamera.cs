using UnityEngine;
using System.Collections;

namespace NaughtyCharacter
{
    public class SafePlayerCamera : MonoBehaviour
    {
        public static SafePlayerCamera Instance;

        [Header("References")]
        public Transform Rig;
        public Transform Pivot;
        public Transform Target;
        public Camera Camera;

        [Header("Camera Feel")]
        [SerializeField] private float sensitivityX = 2.0f;
        [SerializeField] private float sensitivityY = 1.4f;
        [SerializeField] private float rotationSmoothTime = 0.06f;
        [SerializeField] private float followSmoothTime = 0.04f;

        [Header("Pitch Limit")]
        [SerializeField] private float minPitch = -25f;
        [SerializeField] private float maxPitch = 55f;

        [Header("Lock On Camera")]
        [Tooltip("ロックオン機能を使うか")]
        [SerializeField] private bool enableLockOn = true;

        [Tooltip("ロックオン切り替えキー。おすすめはT")]
        [SerializeField] private KeyCode lockOnKey = KeyCode.T;

        [Tooltip("ロックオン対象のタグ")]
        [SerializeField] private string lockOnTargetTag = "Enemy";

        [Tooltip("ここにドラゴンの胸・腹・胴体中心に置いた空オブジェクトを入れる。入っていればRenderer中心より優先される")]
        [SerializeField] private Transform manualLockOnPoint;

        [Tooltip("ロックオンできる最大距離")]
        [SerializeField] private float lockOnRange = 50f;

        [Tooltip("画面中央に近い敵を優先する強さ")]
        [SerializeField] private float screenCenterPriority = 2.0f;

        [Tooltip("ロックオン中のカメラ回転の滑らかさ。大きいほどゆっくり追う")]
        [SerializeField] private float lockOnRotationSmoothTime = 0.16f;

        [Tooltip("ロックオン中、カメラが1秒で回れる最大角度。酔うなら下げる")]
        [SerializeField] private float lockOnMaxTurnSpeed = 220f;

        [Tooltip("manualLockOnPointがない場合、Renderer中心から少し上を見る補正")]
        [SerializeField] private float lockOnCenterHeightOffset = 1.0f;

        [Tooltip("ロックオン中心位置の揺れ補正。大きいほどヌルっと追う")]
        [SerializeField] private float lockOnCenterSmoothTime = 0.18f;

        [Tooltip("この距離以下の中心位置の揺れは無視する。酔い防止")]
        [SerializeField] private float lockOnCenterDeadZone = 0.25f;

        [Tooltip("ロックオン中でもマウス上下操作を少し許可する")]
        [SerializeField] private bool allowPitchInputWhileLockOn = false;

        [Tooltip("ロックオン中の上下視点操作の強さ")]
        [SerializeField] private float lockOnPitchInputSensitivity = 0.25f;

        [Tooltip("水平方向が近すぎる時にYawを更新しない。急な後ろ向きバグ防止")]
        [SerializeField] private float minHorizontalDistanceForYaw = 0.5f;

        [Header("Lock On Marker")]
        [Tooltip("ロックオン中に表示するマークのPrefab。SpriteRenderer推奨")]
        [SerializeField] private GameObject lockOnMarkerPrefab;

        [Tooltip("ロックオンマークを表示するか")]
        [SerializeField] private bool enableLockOnMarker = true;

        [Tooltip("ロックオン開始から何秒間だけマーカーを表示するか")]
        [SerializeField] private float lockOnMarkerVisibleTime = 1.2f;

        [Tooltip("ロックオン中心からの表示位置補正")]
        [SerializeField] private Vector3 lockOnMarkerOffset = Vector3.zero;

        [Tooltip("通常時のロックオンマークの大きさ")]
        [SerializeField] private float lockOnMarkerScale = 1.0f;

        [Tooltip("ロックオン開始時の一瞬の拡大率。1.5なら1.5倍から1倍へ戻る")]
        [SerializeField] private float lockOnMarkerStartPopScale = 1.5f;

        [Tooltip("拡大状態から通常サイズへ戻る時間")]
        [SerializeField] private float lockOnMarkerPopDuration = 0.18f;

        [Tooltip("マークがカメラの方を向くようにする")]
        [SerializeField] private bool lockOnMarkerFaceCamera = true;

        [Tooltip("マークの追従の滑らかさ")]
        [SerializeField] private float lockOnMarkerSmoothTime = 0.04f;

        [Tooltip("マーカーをドラゴンに埋もれにくくするため、カメラ側に少し手前へ出す距離")]
        [SerializeField] private float lockOnMarkerPullTowardCamera = 0.8f;

        [Tooltip("可能ならマーカーをドラゴン越しにも見えるようにする。環境によっては効かない場合あり")]
        [SerializeField] private bool tryMakeMarkerVisibleThroughDragon = true;

        [Tooltip("マーカーの描画順。大きいほど手前に出やすい")]
        [SerializeField] private int lockOnMarkerSortingOrder = 5000;

        [Header("Camera Shake")]
        [SerializeField] private bool enableCameraShake = true;
        [SerializeField] private Transform shakeTarget;
        [SerializeField] private float defaultShakeDuration = 0.25f;
        [SerializeField] private float defaultShakeStrength = 0.15f;

        private float currentYaw;
        private float currentPitch;
        private float targetYaw;
        private float targetPitch;
        private float yawVelocity;
        private float pitchVelocity;
        private Vector3 followVelocity;

        private Vector3 originalShakeLocalPosition;
        private Coroutine shakeCoroutine;

        private Transform currentLockOnTarget;
        private Vector3 smoothedLockOnCenter;
        private Vector3 lockOnCenterVelocity;
        private bool hasLockOnCenter;

        private GameObject lockOnMarkerInstance;
        private Vector3 lockOnMarkerVelocity;
        private Coroutine markerLifetimeCoroutine;
        private Coroutine markerPopCoroutine;
        private Vector3 lockOnMarkerBaseScale;
        private bool markerShouldBeVisible;

        public bool IsLockingOn => currentLockOnTarget != null;
        public Transform CurrentLockOnTarget => currentLockOnTarget;

        private void Awake()
        {
            Instance = this;

            if (Camera == null)
            {
                Camera = GetComponentInChildren<Camera>();
            }

            if (shakeTarget == null)
            {
                shakeTarget = Camera != null ? Camera.transform : transform;
            }

            originalShakeLocalPosition = shakeTarget.localPosition;

            currentYaw = Rig != null ? Rig.localEulerAngles.y : transform.localEulerAngles.y;
            targetYaw = currentYaw;

            currentPitch = Pivot != null ? NormalizeAngle(Pivot.localEulerAngles.x) : 0f;
            targetPitch = currentPitch;
        }

        private void LateUpdate()
        {
            if (Target == null || Rig == null || Pivot == null)
            {
                return;
            }

            Rig.position = Vector3.SmoothDamp(
                Rig.position,
                Target.position,
                ref followVelocity,
                followSmoothTime
            );

            HandleLockOnInput();

            if (IsLockingOn)
            {
                UpdateLockOnCamera();
            }
            else
            {
                UpdateNormalCamera();
            }
        }

        private void HandleLockOnInput()
        {
            if (!enableLockOn) return;

            if (Input.GetKeyDown(lockOnKey))
            {
                if (currentLockOnTarget != null)
                {
                    ClearLockOn();
                }
                else
                {
                    TryLockOn();
                }
            }

            if (currentLockOnTarget != null)
            {
                Vector3 center = GetRawLockOnCenter(currentLockOnTarget);
                float distance = Vector3.Distance(Target.position, center);

                if (distance > lockOnRange || !currentLockOnTarget.gameObject.activeInHierarchy)
                {
                    ClearLockOn();
                }
            }
        }

        private void TryLockOn()
        {
            Transform bestTarget = FindBestLockOnTarget();

            if (bestTarget != null)
            {
                currentLockOnTarget = bestTarget;

                smoothedLockOnCenter = GetRawLockOnCenter(currentLockOnTarget);
                lockOnCenterVelocity = Vector3.zero;
                hasLockOnCenter = true;

                ShowLockOnMarker(smoothedLockOnCenter);
            }
        }

        private Transform FindBestLockOnTarget()
        {
            if (manualLockOnPoint != null)
            {
                float manualDistance = Vector3.Distance(Target.position, manualLockOnPoint.position);

                if (manualDistance <= lockOnRange)
                {
                    return manualLockOnPoint;
                }
            }

            GameObject[] enemies = GameObject.FindGameObjectsWithTag(lockOnTargetTag);

            Transform bestTarget = null;
            float bestScore = Mathf.Infinity;

            foreach (GameObject enemy in enemies)
            {
                if (enemy == null) continue;
                if (!enemy.activeInHierarchy) continue;

                Vector3 enemyCenter = GetRawLockOnCenter(enemy.transform);
                float distance = Vector3.Distance(Target.position, enemyCenter);

                if (distance > lockOnRange) continue;

                float score = distance;

                if (Camera != null)
                {
                    Vector3 viewportPoint = Camera.WorldToViewportPoint(enemyCenter);

                    if (viewportPoint.z < 0f)
                    {
                        continue;
                    }

                    float screenCenterDistance = Vector2.Distance(
                        new Vector2(viewportPoint.x, viewportPoint.y),
                        new Vector2(0.5f, 0.5f)
                    );

                    score += screenCenterDistance * screenCenterPriority * 10f;
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    bestTarget = enemy.transform;
                }
            }

            return bestTarget;
        }

        private Vector3 GetRawLockOnCenter(Transform target)
        {
            if (manualLockOnPoint != null)
            {
                return manualLockOnPoint.position;
            }

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();

            if (renderers == null || renderers.Length == 0)
            {
                return target.position + Vector3.up * lockOnCenterHeightOffset;
            }

            Bounds bounds = renderers[0].bounds;

            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            Vector3 center = bounds.center;
            center += Vector3.up * lockOnCenterHeightOffset;

            return center;
        }

        private Vector3 GetSmoothedLockOnCenter()
        {
            Vector3 rawCenter = GetRawLockOnCenter(currentLockOnTarget);

            if (!hasLockOnCenter)
            {
                smoothedLockOnCenter = rawCenter;
                hasLockOnCenter = true;
                return smoothedLockOnCenter;
            }

            float centerMoveDistance = Vector3.Distance(smoothedLockOnCenter, rawCenter);

            if (centerMoveDistance <= lockOnCenterDeadZone)
            {
                return smoothedLockOnCenter;
            }

            smoothedLockOnCenter = Vector3.SmoothDamp(
                smoothedLockOnCenter,
                rawCenter,
                ref lockOnCenterVelocity,
                lockOnCenterSmoothTime
            );

            return smoothedLockOnCenter;
        }

        private void ClearLockOn()
        {
            currentLockOnTarget = null;
            hasLockOnCenter = false;
            lockOnCenterVelocity = Vector3.zero;

            HideLockOnMarker();
        }

        private void ShowLockOnMarker(Vector3 position)
        {
            if (!enableLockOnMarker) return;
            if (lockOnMarkerPrefab == null) return;

            if (lockOnMarkerInstance == null)
            {
                lockOnMarkerInstance = Instantiate(lockOnMarkerPrefab);

                lockOnMarkerBaseScale = lockOnMarkerPrefab.transform.localScale * lockOnMarkerScale;
                lockOnMarkerInstance.transform.localScale = lockOnMarkerBaseScale;

                PrepareLockOnMarkerRenderer(lockOnMarkerInstance);
            }

            markerShouldBeVisible = true;

            Vector3 finalPosition = GetMarkerVisiblePosition(position + lockOnMarkerOffset);
            lockOnMarkerInstance.transform.position = finalPosition;
            lockOnMarkerInstance.SetActive(true);

            if (markerLifetimeCoroutine != null)
            {
                StopCoroutine(markerLifetimeCoroutine);
            }

            if (markerPopCoroutine != null)
            {
                StopCoroutine(markerPopCoroutine);
            }

            markerPopCoroutine = StartCoroutine(LockOnMarkerPopRoutine());
            markerLifetimeCoroutine = StartCoroutine(LockOnMarkerLifetimeRoutine());
        }

        private void HideLockOnMarker()
        {
            markerShouldBeVisible = false;

            if (markerLifetimeCoroutine != null)
            {
                StopCoroutine(markerLifetimeCoroutine);
                markerLifetimeCoroutine = null;
            }

            if (markerPopCoroutine != null)
            {
                StopCoroutine(markerPopCoroutine);
                markerPopCoroutine = null;
            }

            if (lockOnMarkerInstance != null)
            {
                lockOnMarkerInstance.SetActive(false);
            }
        }

        private IEnumerator LockOnMarkerLifetimeRoutine()
        {
            float timer = 0f;

            while (timer < lockOnMarkerVisibleTime)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            markerShouldBeVisible = false;

            if (lockOnMarkerInstance != null)
            {
                lockOnMarkerInstance.SetActive(false);
            }

            markerLifetimeCoroutine = null;
        }

        private IEnumerator LockOnMarkerPopRoutine()
        {
            if (lockOnMarkerInstance == null)
            {
                yield break;
            }

            Vector3 startScale = lockOnMarkerBaseScale * lockOnMarkerStartPopScale;
            Vector3 endScale = lockOnMarkerBaseScale;

            lockOnMarkerInstance.transform.localScale = startScale;

            float timer = 0f;

            while (timer < lockOnMarkerPopDuration)
            {
                timer += Time.deltaTime;

                float t = Mathf.Clamp01(timer / Mathf.Max(lockOnMarkerPopDuration, 0.001f));
                float easedT = 1f - Mathf.Pow(1f - t, 3f);

                if (lockOnMarkerInstance != null)
                {
                    lockOnMarkerInstance.transform.localScale = Vector3.Lerp(startScale, endScale, easedT);
                }

                yield return null;
            }

            if (lockOnMarkerInstance != null)
            {
                lockOnMarkerInstance.transform.localScale = endScale;
            }

            markerPopCoroutine = null;
        }

        private void PrepareLockOnMarkerRenderer(GameObject marker)
        {
            if (marker == null) return;

            SpriteRenderer[] spriteRenderers = marker.GetComponentsInChildren<SpriteRenderer>(true);

            foreach (SpriteRenderer spriteRenderer in spriteRenderers)
            {
                if (spriteRenderer == null) continue;

                spriteRenderer.sortingOrder = lockOnMarkerSortingOrder;

                if (tryMakeMarkerVisibleThroughDragon)
                {
                    Shader alwaysVisibleShader = Shader.Find("GUI/Text Shader");

                    if (alwaysVisibleShader != null)
                    {
                        Material alwaysVisibleMaterial = new Material(alwaysVisibleShader);
                        alwaysVisibleMaterial.renderQueue = 5000;
                        spriteRenderer.material = alwaysVisibleMaterial;
                    }
                    else
                    {
                        spriteRenderer.material.renderQueue = 5000;
                    }
                }
            }

            Renderer[] renderers = marker.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;
                renderer.sortingOrder = lockOnMarkerSortingOrder;

                if (renderer.material != null)
                {
                    renderer.material.renderQueue = 5000;
                }
            }
        }

        private void UpdateLockOnMarker(Vector3 centerPosition)
        {
            if (!enableLockOnMarker) return;
            if (lockOnMarkerPrefab == null) return;
            if (!markerShouldBeVisible) return;

            if (lockOnMarkerInstance == null)
            {
                ShowLockOnMarker(centerPosition);
                return;
            }

            if (!lockOnMarkerInstance.activeSelf)
            {
                lockOnMarkerInstance.SetActive(true);
            }

            Vector3 targetPosition = GetMarkerVisiblePosition(centerPosition + lockOnMarkerOffset);

            lockOnMarkerInstance.transform.position = Vector3.SmoothDamp(
                lockOnMarkerInstance.transform.position,
                targetPosition,
                ref lockOnMarkerVelocity,
                lockOnMarkerSmoothTime
            );

            if (lockOnMarkerFaceCamera && Camera != null)
            {
                Vector3 directionToCamera = lockOnMarkerInstance.transform.position - Camera.transform.position;

                if (directionToCamera.sqrMagnitude > 0.001f)
                {
                    lockOnMarkerInstance.transform.rotation = Quaternion.LookRotation(directionToCamera.normalized, Vector3.up);
                }
            }
        }

        private Vector3 GetMarkerVisiblePosition(Vector3 worldPosition)
        {
            if (Camera == null)
            {
                return worldPosition;
            }

            Vector3 directionToCamera = Camera.transform.position - worldPosition;

            if (directionToCamera.sqrMagnitude < 0.001f)
            {
                return worldPosition;
            }

            return worldPosition + directionToCamera.normalized * lockOnMarkerPullTowardCamera;
        }

        private void UpdateNormalCamera()
        {
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            targetYaw += mouseX * sensitivityX;
            targetPitch -= mouseY * sensitivityY;
            targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);

            currentYaw = Mathf.SmoothDampAngle(
                currentYaw,
                targetYaw,
                ref yawVelocity,
                rotationSmoothTime
            );

            currentPitch = Mathf.SmoothDampAngle(
                currentPitch,
                targetPitch,
                ref pitchVelocity,
                rotationSmoothTime
            );

            Rig.localRotation = Quaternion.Euler(0f, currentYaw, 0f);
            Pivot.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
        }

        private void UpdateLockOnCamera()
        {
            if (currentLockOnTarget == null)
            {
                ClearLockOn();
                return;
            }

            Vector3 playerPos = Target.position;
            Vector3 enemyCenter = GetSmoothedLockOnCenter();

            UpdateLockOnMarker(enemyCenter);

            Vector3 direction = enemyCenter - playerPos;

            if (direction.sqrMagnitude < 0.01f)
            {
                return;
            }

            Vector3 flatDirection = new Vector3(direction.x, 0f, direction.z);
            float horizontalDistance = flatDirection.magnitude;

            float desiredYaw = targetYaw;

            if (horizontalDistance > minHorizontalDistanceForYaw)
            {
                desiredYaw = Quaternion.LookRotation(flatDirection.normalized, Vector3.up).eulerAngles.y;
            }

            float desiredPitch = -Mathf.Atan2(direction.y, Mathf.Max(horizontalDistance, 0.01f)) * Mathf.Rad2Deg;
            desiredPitch = Mathf.Clamp(desiredPitch, minPitch, maxPitch);

            if (allowPitchInputWhileLockOn && Cursor.lockState == CursorLockMode.Locked)
            {
                float mouseY = Input.GetAxis("Mouse Y");
                desiredPitch -= mouseY * lockOnPitchInputSensitivity;
                desiredPitch = Mathf.Clamp(desiredPitch, minPitch, maxPitch);
            }

            targetYaw = Mathf.MoveTowardsAngle(
                targetYaw,
                desiredYaw,
                lockOnMaxTurnSpeed * Time.deltaTime
            );

            targetPitch = Mathf.MoveTowardsAngle(
                targetPitch,
                desiredPitch,
                lockOnMaxTurnSpeed * Time.deltaTime
            );

            currentYaw = Mathf.SmoothDampAngle(
                currentYaw,
                targetYaw,
                ref yawVelocity,
                lockOnRotationSmoothTime
            );

            currentPitch = Mathf.SmoothDampAngle(
                currentPitch,
                targetPitch,
                ref pitchVelocity,
                lockOnRotationSmoothTime
            );

            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

            Rig.localRotation = Quaternion.Euler(0f, currentYaw, 0f);
            Pivot.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
        }

        private float NormalizeAngle(float angle)
        {
            angle %= 360f;

            if (angle > 180f)
            {
                angle -= 360f;
            }

            return angle;
        }

        public void Shake()
        {
            Shake(defaultShakeDuration, defaultShakeStrength);
        }

        public void Shake(float duration, float strength)
        {
            if (!enableCameraShake) return;
            if (shakeTarget == null) return;

            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
            }

            shakeCoroutine = StartCoroutine(ShakeRoutine(duration, strength));
        }

        private IEnumerator ShakeRoutine(float duration, float strength)
        {
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;

                Vector3 randomOffset = Random.insideUnitSphere * strength;
                randomOffset.z = 0f;

                shakeTarget.localPosition = originalShakeLocalPosition + randomOffset;

                yield return null;
            }

            shakeTarget.localPosition = originalShakeLocalPosition;
            shakeCoroutine = null;
        }
    }
}