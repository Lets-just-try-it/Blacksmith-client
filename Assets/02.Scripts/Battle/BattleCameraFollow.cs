using Blacksmith.Core.Battle;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 스테이지마다 앞으로 전진하는 모험가 파티의 중심을 따라간다.
    public class BattleCameraFollow : MonoBehaviour
    {
        [SerializeField] private BattlePlayer _player;
        [SerializeField] private Vector3 _offset = new Vector3(-6f, 9f, -9f);
        [SerializeField, Min(0f)] private float _smoothTime = 0.4f;

        private Vector3 _velocity;

        private void LateUpdate()
        {
            if (!TryGetPartyCenter(out Vector3 center))
            {
                return;
            }

            transform.position = Vector3.SmoothDamp(transform.position, center + _offset, ref _velocity, _smoothTime);
        }

        private bool TryGetPartyCenter(out Vector3 center)
        {
            center = Vector3.zero;
            int count = 0;
            foreach (BattleUnitView view in _player.Views)
            {
                if (view.Unit.Team == BattleTeam.Adventurer && view.Unit.IsAlive)
                {
                    center += view.transform.position;
                    count++;
                }
            }

            if (count == 0)
            {
                return false;
            }

            center /= count;
            return true;
        }
    }
}
