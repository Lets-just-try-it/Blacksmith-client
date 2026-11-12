using UnityEngine;

namespace Blacksmith.Utils
{
    // 부모가 돌아도 이 오브젝트는 카메라 쪽으로 기울여 둔 방향을 유지한다. 걸어 다니는 NPC의 이름표, 상호작용 안내 등에 붙인다.
    // 프리팹에서 부모가 회전하지 않은 상태로 맞춰 둔 로컬 회전을 월드 회전으로 고정한다.
    public class KeepWorldRotation : MonoBehaviour
    {
        private Quaternion _worldRotation;

        private void Awake()
        {
            _worldRotation = transform.localRotation;
        }

        private void LateUpdate()
        {
            transform.rotation = _worldRotation;
        }
    }
}
