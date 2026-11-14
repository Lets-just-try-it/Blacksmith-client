using System;
using UnityEngine;

namespace Blacksmith.Plaza
{
    // 여관 대화 결과와 씬의 화면을 잇는 에셋이다. Player 프리팹의 InnPlayerActions가 내 플레이어일 때 등록하고,
    // 여관 모험가가 "장비 건네 주기"를 고른 플레이어 화면에서 장비 착용 창을 열 때도 이 에셋을 거친다.
    [CreateAssetMenu(fileName = "InnChannel", menuName = "Blacksmith/Runtime/Inn Channel")]
    public class InnChannel : ScriptableObject
    {
        // 내 플레이어의 여관 행동. 내 플레이어가 스폰되기 전이나 디스폰된 뒤에는 null이다.
        public InnPlayerActions LocalActions { get; private set; }

        // 내 화면에서만 불린다. 장비를 건넬 모험가 번호다.
        public event Action<int> GiftRequested;

        public void Register(InnPlayerActions actions) => LocalActions = actions;
        public void Unregister(InnPlayerActions actions) => LocalActions = LocalActions == actions ? null : LocalActions;

        public void RequestGift(int adventurerId)
        {
            GiftRequested?.Invoke(adventurerId);
        }
    }
}
