using Blacksmith.Core.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace Blacksmith.Battle
{
    // 화면 하단의 모험가 초상 한 칸. 스킬 버튼이 아니라 HP·마나 표시만 한다.
    public class BattleHudSlot : MonoBehaviour
    {
        [SerializeField] private Image _portrait;
        [SerializeField] private Image _hpFill;
        [SerializeField] private Image _manaFill;
        [SerializeField] private GameObject _fallenOverlay;

        private BattleUnit _unit;

        public void Bind(BattleUnit unit, Sprite portrait)
        {
            _unit = unit;
            if (_portrait != null && portrait != null)
            {
                _portrait.sprite = portrait;
            }

            Refresh();
        }

        public void Refresh()
        {
            if (_unit == null)
            {
                return;
            }

            _hpFill.fillAmount = (float)_unit.Hp / _unit.Stats.MaxHp;
            _manaFill.fillAmount = _unit.Stats.MaxMana > 0 ? (float)_unit.Mana / _unit.Stats.MaxMana : 0f;
            if (_fallenOverlay != null)
            {
                _fallenOverlay.SetActive(_unit.IsFallen);
            }
        }
    }
}
