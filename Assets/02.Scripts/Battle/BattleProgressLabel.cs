using TMPro;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 토벌 진행도를 "토벌 3/8"처럼 보여 준다 (docs/GAME_CONCEPT.md 7.1).
    public class BattleProgressLabel : MonoBehaviour
    {
        [SerializeField] private BattlePlayer _player;
        [SerializeField] private TMP_Text _text;

        private void Awake()
        {
            _text.text = "";
        }

        private void OnEnable()
        {
            _player.ProgressChanged += HandleProgressChanged;
        }

        private void OnDisable()
        {
            _player.ProgressChanged -= HandleProgressChanged;
        }

        private void HandleProgressChanged(int defeated, int total)
        {
            _text.text = $"토벌 {defeated}/{total}";
        }
    }
}
