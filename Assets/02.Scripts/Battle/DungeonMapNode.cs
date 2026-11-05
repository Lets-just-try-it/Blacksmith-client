using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blacksmith.Battle
{
    // 던전 지도의 거점 하나(스테이지 또는 보스 스테이지). 누르면 지도 창이 그 단계와 스테이지를 고른다.
    public class DungeonMapNode : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        [Tooltip("보스 거점에만 둔다. 비워 두면 이름을 보여 주지 않는다")]
        [SerializeField] private TMP_Text _label;
        [Tooltip("고른 거점에만 켠다")]
        [SerializeField] private GameObject _selectedMark;

        public event Action<DungeonMapNode> Clicked;

        public int Index { get; private set; }

        private void Awake()
        {
            _button.onClick.AddListener(() => Clicked?.Invoke(this));
        }

        public void Show(int index, string label, Color color, bool canSelect, bool isSelected)
        {
            Index = index;
            if (_label != null)
            {
                _label.text = label;
            }

            _background.color = color;
            _button.interactable = canSelect;
            _selectedMark.SetActive(isSelected);
        }
    }
}
