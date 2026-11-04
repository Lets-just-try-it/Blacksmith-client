using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blacksmith.Battle
{
    // 파견 창의 모험가 한 줄. 줄을 누르면 파티에 넣고 빼며, 장비 버튼으로 장비 착용 창을 연다.
    public class DispatchAdventurerRow : MonoBehaviour
    {
        [SerializeField] private Button _selectButton;
        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _statsText;
        [SerializeField] private Button _equipButton;

        public event Action<DispatchAdventurerRow> SelectClicked;
        public event Action<DispatchAdventurerRow> EquipClicked;

        public int AdventurerId { get; private set; }

        private void Awake()
        {
            _selectButton.onClick.AddListener(() => SelectClicked?.Invoke(this));
            _equipButton.onClick.AddListener(() => EquipClicked?.Invoke(this));
        }

        public void Show(int adventurerId, string nameLine, string statsLine, Color color, bool canSelect)
        {
            AdventurerId = adventurerId;
            _nameText.text = nameLine;
            _statsText.text = statsLine;
            _background.color = color;
            _selectButton.interactable = canSelect;
        }
    }
}
