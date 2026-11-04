using Blacksmith.Core.Battle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blacksmith.Battle
{
    // 보스 스테이지에서 화면 상단에 보스 이름과 체력을 보여 준다. 보스가 분노하면 이름 옆에 표시하고 바 색을 바꾼다.
    public class BattleBossHpBar : MonoBehaviour
    {
        [SerializeField] private BattlePlayer _player;
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private RectTransform _fill;
        [SerializeField] private Image _fillImage;
        [SerializeField] private Color _enragedFillColor = new Color(1f, 0.45f, 0.1f, 1f);
        [SerializeField] private string _enragedLabel = "분노";

        private BattleUnit _boss;
        private string _bossName;
        private Color _normalFillColor;

        private void Awake()
        {
            _normalFillColor = _fillImage.color;
            Hide();
        }

        private void OnEnable()
        {
            _player.TickAdvanced += Refresh;
            _player.UnitEnraged += HandleUnitEnraged;
        }

        private void OnDisable()
        {
            _player.TickAdvanced -= Refresh;
            _player.UnitEnraged -= HandleUnitEnraged;
        }

        public void Show(BattleUnit boss, string bossName)
        {
            _boss = boss;
            _bossName = bossName;
            _nameText.text = bossName;
            _fillImage.color = _normalFillColor;
            _root.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            _boss = null;
            _root.SetActive(false);
        }

        private void HandleUnitEnraged(BattleUnitView view)
        {
            if (view.Unit != _boss)
            {
                return;
            }

            _nameText.text = $"{_bossName} - {_enragedLabel}";
            _fillImage.color = _enragedFillColor;
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
