using Blacksmith.Core.Battle;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 잡몹 발밑에 붙는 월드 공간 체력바. 보스 체력은 화면 상단의 BattleBossHpBar가 보여 준다.
    public class BattleUnitHpBar : MonoBehaviour
    {
        [SerializeField] private RectTransform _fill;
        [Tooltip("땅에 묻히지 않도록 카메라 쪽으로 당기는 거리")]
        [SerializeField, Min(0f)] private float _pullTowardCamera = 0.6f;

        private Vector3 _basePosition;

        private void Awake()
        {
            _basePosition = transform.localPosition;
        }

        // 쿼터뷰 카메라는 회전하지 않아 스폰할 때 한 번만 맞춘다.
        public void FaceCamera(Quaternion cameraRotation)
        {
            transform.rotation = cameraRotation;
            transform.localPosition = _basePosition;
            transform.position -= cameraRotation * Vector3.forward * _pullTowardCamera;
        }

        public void Refresh(BattleUnit unit)
        {
            BarFill.SetRatio(_fill, (float)unit.Hp / unit.Stats.MaxHp);
            gameObject.SetActive(unit.IsAlive);
        }
    }
}
