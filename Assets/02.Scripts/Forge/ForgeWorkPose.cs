using System;
using Blacksmith.Characters;
using Blacksmith.Core.Forge;
using Blacksmith.Inventory;
using Fusion;
using UnityEngine;

namespace Blacksmith.Forge
{
    // Player 프리팹에 붙는다. 이 플레이어가 작업대를 쓰는 동안(미니게임 중) 작업대 종류에 맞는 작업 동작과 손에 든 도구를 보여 주고,
    // 인벤토리에서 들고 있던 물건은 숨긴다.
    // 작업대 사용권은 호스트가 복제하므로 모든 참가자의 화면에서 같은 모습이 된다. 표시 전용이라 Render에서 읽기만 한다.
    public class ForgeWorkPose : NetworkBehaviour
    {
        // 작업대 종류별 모습. Pose는 애니메이터의 WorkPose 값, HeldPart는 모델 안에서 켤 도구 파츠 이름이다.
        [Serializable]
        public class WorkStyle
        {
            public CraftingStationKind Kind;
            public int Pose = 1;
            public string HeldPart = "Weapon_SmithHammer_Held";
        }

        [SerializeField] private ForgeStationRegistry _registry;
        [SerializeField] private HumanoidPose _pose;
        [SerializeField] private HumanoidAppearanceView _view;
        [Tooltip("작업하는 동안 숨길 인벤토리의 든 물건 표시")]
        [SerializeField] private HeldItemView _heldItem;
        [Tooltip("목록에 없는 작업대 종류는 작업 동작 없이 둔다")]
        [SerializeField] private WorkStyle[] _styles = new WorkStyle[0];

        private ForgeStationState _shownStation;
        private bool _hasShown;

        public override void Render()
        {
            _registry.TryFindUsedBy(Object.InputAuthority, out ForgeStationState station);
            if (_hasShown && station == _shownStation)
            {
                return;
            }

            _hasShown = true;
            _shownStation = station;
            WorkStyle style = station != null ? FindStyle(station.Kind) : null;
            _pose.SetWork(style != null ? style.Pose : HumanoidPose.NoWork);
            _view.SetHeldPart(style?.HeldPart);
            _heldItem.SetHidden(station != null);
        }

        private WorkStyle FindStyle(CraftingStationKind kind)
        {
            foreach (WorkStyle style in _styles)
            {
                if (style.Kind == kind)
                {
                    return style;
                }
            }

            return null;
        }
    }
}
