using UnityEngine;
using NaughtyCharacter;
using System.Collections;

namespace Retro.ThirdPersonCharacter
{
    [RequireComponent(typeof(PlayerInput), typeof(Animator), typeof(Movement))]
    public class Combat : MonoBehaviour
    {
        private const string attackTriggerName = "Attack";
        private const string specialAttackTriggerName = "Ability";

        private Animator _animator;
        private PlayerInput _playerInput;
        private Movement _movement;

        public bool AttackInProgress { get; private set; } = false;
        private bool _comboChainAllowed = false;

        [SerializeField] private WeaponHitbox daggerHitbox;
        [SerializeField] private WeaponHitbox swordHitbox;

        [Header("タイミング設定 (秒)")]
        [SerializeField] private float daggerStart = 0.2f;
        [SerializeField] private float daggerEnd = 0.5f;
        [SerializeField] private float swordStart = 4f;
        [SerializeField] private float swordEnd = 6f;

        private void Start()
        {
            _animator = GetComponent<Animator>();
            _playerInput = GetComponent<PlayerInput>();
            _movement = GetComponent<Movement>();
        }

        private void Update()
        {
            // ログで入力を可視化
            if (Input.GetMouseButtonDown(0)) Debug.Log("左クリック検知");
            if (Input.GetMouseButtonDown(1)) Debug.Log("右クリック検知");

            // 実際の攻撃処理
            if (_playerInput.AttackInput && !AttackInProgress)
            {
                Attack();
            }
            else if (_playerInput.SpecialAttackInput && (!AttackInProgress || _comboChainAllowed))
            {
                SpecialAttack();
            }
        }

        // Animationイベント用（念のため残しつつ、中身は空に）
        public void StartDaggerHit() { }
        public void EndDaggerHit() { }
        public void StartSwordHit() { }
        public void EndSwordHit() { }

        // 状態管理用
        private void SetAttackStart()
        {
            AttackInProgress = true;
            _comboChainAllowed = true;
            if (_movement != null) _movement.isAttacking = true;
        }

        private void SetAttackEnd()
        {
            AttackInProgress = false;
            _comboChainAllowed = false;
            _animator.ResetTrigger(attackTriggerName);
            _animator.ResetTrigger(specialAttackTriggerName);
            if (_movement != null) _movement.isAttacking = false;
        }

        private void Attack()
        {
            Debug.Log("弱攻撃実行中");
            _animator.SetTrigger(attackTriggerName);
            StartCoroutine(ControlHitbox(daggerHitbox, daggerStart, daggerEnd));
        }

        private void SpecialAttack()
        {
            Debug.Log("強攻撃実行中");
            _animator.SetTrigger(specialAttackTriggerName);
            _comboChainAllowed = false;
            StartCoroutine(ControlHitbox(swordHitbox, swordStart, swordEnd));
        }

        private IEnumerator ControlHitbox(WeaponHitbox hitbox, float startDelay, float endDelay)
        {
            if (hitbox == null)
            {
                Debug.LogError("Hitboxが空です！");
                yield break;
            }

            yield return new WaitForSeconds(startDelay);
            Debug.Log("判定ON");
            hitbox.EnableHitbox();

            yield return new WaitForSeconds(endDelay - startDelay);
            Debug.Log("判定OFF");
            hitbox.DisableHitbox();
        }
    }
}