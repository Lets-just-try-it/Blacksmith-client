using Blacksmith.Core.Battle;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 전투 유닛 하나의 표시만 맡는다. 상태는 BattleSimulation의 BattleUnit을 읽기만 한다.
    public class BattleUnitView : MonoBehaviour
    {
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int SkillHash = Animator.StringToHash("Skill");
        private static readonly int HitHash = Animator.StringToHash("Hit");
        private static readonly int FallHash = Animator.StringToHash("Fall");
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Animator _animator;
        [Tooltip("애니메이터가 없는 임시 모델에서 쓰러질 때 눕힐 대상")]
        [SerializeField] private Transform _body;
        [SerializeField] private ParticleSystem _attackEffect;
        [SerializeField] private ParticleSystem _skillEffect;
        [SerializeField] private ParticleSystem _hitEffect;
        [SerializeField] private ParticleSystem _healEffect;
        [SerializeField] private ParticleSystem _enrageEffect;
        [Tooltip("살아 있는 동안 계속 켜 두는 이펙트. 보스만 쓴다.")]
        [SerializeField] private ParticleSystem _aura;
        [Tooltip("분노한 뒤 계속 켜 두는 이펙트. 보스만 쓴다.")]
        [SerializeField] private ParticleSystem _enrageAura;
        [Tooltip("분노하면 몸 색을 이 색으로 물들인다. 비우면 색을 바꾸지 않는다.")]
        [SerializeField] private Renderer _bodyRenderer;
        [SerializeField] private Color _enrageTint = new Color(0.85f, 0.1f, 0.05f, 1f);
        [Tooltip("잡몹만 쓴다. 비워 두면 체력바를 표시하지 않는다.")]
        [SerializeField] private BattleUnitHpBar _hpBar;

        public BattleUnit Unit { get; private set; }

        public void Bind(BattleUnit unit, Quaternion cameraRotation)
        {
            Unit = unit;
            if (_hpBar != null)
            {
                _hpBar.FaceCamera(cameraRotation);
                _hpBar.Refresh(unit);
            }
        }

        public void RefreshHp()
        {
            if (_hpBar != null)
            {
                _hpBar.Refresh(Unit);
            }
        }

        public void SetMoving(bool isMoving)
        {
            if (_animator != null)
            {
                _animator.SetBool(IsMovingHash, isMoving);
            }
        }

        public void PlayAttack()
        {
            Trigger(AttackHash);
            Play(_attackEffect);
        }

        public void PlaySkill()
        {
            Trigger(SkillHash);
            Play(_skillEffect);
        }

        public void PlayHit()
        {
            Trigger(HitHash);
            Play(_hitEffect);
        }

        public void PlayHeal()
        {
            Play(_healEffect);
        }

        public void PlayEnrage()
        {
            Play(_enrageEffect);
            Play(_enrageAura);
            TintBody(_enrageTint);
        }

        public void PlayFall()
        {
            Trigger(FallHash);
            SetMoving(false);
            Stop(_aura);
            Stop(_enrageAura);
            if (_animator == null && _body != null)
            {
                _body.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
        }

        private void Trigger(int hash)
        {
            if (_animator != null)
            {
                _animator.SetTrigger(hash);
            }
        }

        private void TintBody(Color color)
        {
            if (_bodyRenderer == null)
            {
                return;
            }

            var block = new MaterialPropertyBlock();
            _bodyRenderer.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            _bodyRenderer.SetPropertyBlock(block);
        }

        private static void Stop(ParticleSystem effect)
        {
            if (effect != null)
            {
                effect.Stop();
            }
        }

        private static void Play(ParticleSystem effect)
        {
            if (effect != null)
            {
                effect.Play();
            }
        }
    }
}
