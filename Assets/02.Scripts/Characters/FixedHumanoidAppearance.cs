using Blacksmith.Core.Characters;
using UnityEngine;

namespace Blacksmith.Characters
{
    // 정해 둔 외형을 그대로 보여 준다. 플레이어처럼 외형을 무작위로 뽑지 않는 캐릭터가 쓴다.
    // [고도화] 플레이어 외형 커스터마이즈가 생기면 동기화한 외형으로 바꾼다.
    public class FixedHumanoidAppearance : MonoBehaviour
    {
        [SerializeField] private HumanoidAppearanceView _view;
        [SerializeField] private HumanoidAppearance _appearance;

        // 대장간 작업 자세처럼 스폰 직후 손에 든 파츠를 바꾸는 표시가 덮어써지지 않도록 가장 먼저 적용한다.
        private void Awake()
        {
            _view.Apply(_appearance);
        }
    }
}
