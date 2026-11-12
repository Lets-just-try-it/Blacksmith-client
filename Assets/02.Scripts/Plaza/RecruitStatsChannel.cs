using Blacksmith.Core.Adventurers;
using UnityEngine;

namespace Blacksmith.Plaza
{
    // 런타임에 스폰되는 영입 후보와 씬의 스탯 창을 잇는다. 후보 프리팹은 씬 오브젝트를 참조할 수 없으므로
    // 씬의 RecruitStatsPanel이 켜질 때 여기에 자신을 등록하고, 후보는 이 에셋만 참조한다.
    [CreateAssetMenu(fileName = "RecruitStatsChannel", menuName = "Blacksmith/Runtime/Recruit Stats Channel")]
    public class RecruitStatsChannel : ScriptableObject
    {
        private RecruitStatsPanel _panel;
        // 지금 창을 띄운 후보. 다른 후보의 Hide가 이 창을 닫지 않게 한다.
        private Object _owner;

        public void Register(RecruitStatsPanel panel)
        {
            _panel = panel;
            _owner = null;
        }

        public void Unregister(RecruitStatsPanel panel)
        {
            if (_panel == panel)
            {
                _panel = null;
                _owner = null;
            }
        }

        public void Show(Object owner, AdventurerData adventurer)
        {
            if (_panel == null)
            {
                return;
            }

            _owner = owner;
            _panel.Show(adventurer);
        }

        public void Hide(Object owner)
        {
            if (_panel == null || _owner != owner)
            {
                return;
            }

            _owner = null;
            _panel.Hide();
        }
    }
}
