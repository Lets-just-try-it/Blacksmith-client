using TMPro;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 지금 싸우는 웨이브를 "웨이브 2/3"처럼 보여 준다.
    public class BattleWaveLabel : MonoBehaviour
    {
        [SerializeField] private BattlePlayer _player;
        [SerializeField] private TMP_Text _text;

        private void Awake()
        {
            _text.text = "";
        }

        private void OnEnable()
        {
            _player.WaveStarted += HandleWaveStarted;
        }

        private void OnDisable()
        {
            _player.WaveStarted -= HandleWaveStarted;
        }

        private void HandleWaveStarted(int wave, int waveCount)
        {
            _text.text = $"웨이브 {wave + 1}/{waveCount}";
        }
    }
}
