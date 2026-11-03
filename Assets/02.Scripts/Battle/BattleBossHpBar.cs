using Blacksmith.Core.Battle;
using TMPro;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 보스 스테이지에서 화면 상단에 보스 이름과 체력을 보여 준다.
    public class BattleBossHpBar : MonoBehaviour
    {
        [SerializeField] private BattlePlayer _player;
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private RectTransform _fill;

        private BattleUnit _boss;

        private void Awake()
        {
            Hide();
        }

        private void OnEnable()
        {
            _player.TickAdvanced += Refresh;
        }

        private void OnDisable()
        {
            _player.TickAdvanced -= Refresh;
        }

        public void Show(BattleUnit boss, string bossName)
        {
            _boss = boss;
            _nameText.text = bossName;
            _root.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            _boss = null;
            _root.SetActive(false);
        }

        private void Refresh()
        {
            if (_boss == null)
            {
                return;
            }

            BarFill.SetRatio(_fill, (float)_boss.Hp / _boss.Stats.MaxHp);
            if (_boss.IsFallen)
            {
                Hide();
            }
        }
    }
}
