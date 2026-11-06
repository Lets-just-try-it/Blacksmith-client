using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blacksmith.Forge
{
    // 제작 목록의 한 줄. 이름과 티어를 보여 주고, 지금 작업대 슬롯의 재료로 만들 수 있으면 표시한다.
    public class CraftingListRow : MonoBehaviour
    {
        private static readonly Color Normal = new Color(0.12f, 0.11f, 0.1f, 0.9f);
        private static readonly Color Selected = new Color(0.3f, 0.22f, 0.1f, 0.95f);

        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _tierText;
        [SerializeField] private Image _readyMark;

        private Action<int> _onClick;
        private int _index;

        private void Awake()
        {
            _button.onClick.AddListener(OnClicked);
        }

        public void Bind(int index, string itemName, string tierLabel, Action<int> onClick)
        {
            _index = index;
            _onClick = onClick;
            _nameText.text = itemName;
            _tierText.text = tierLabel;
            SetSelected(false);
            SetReady(false);
        }

        public void SetSelected(bool selected)
        {
            _background.color = selected ? Selected : Normal;
        }

        // 재료가 다 놓였으면 초록 표시를 켠다.
        public void SetReady(bool ready)
        {
            _readyMark.color = ready ? new Color(0.4f, 1f, 0.55f) : new Color(1f, 1f, 1f, 0.12f);
        }

        private void OnClicked()
        {
            _onClick?.Invoke(_index);
        }
    }
}
