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
        [Tooltip("오른쪽 앵커를 체력 비율로 줄인다. 긴 바에서 둥근 스프라이트가 늘어나 보이지 않도록 Filled 대신 쓴다.")]
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

            _fill.anchorMax = new Vector2((float)_boss.Hp / _boss.Stats.MaxHp, _fill.anchorMax.y);
            if (_boss.IsFallen)
            {
                Hide();
            }
        }
    }
}
