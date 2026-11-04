using Blacksmith.Network;
using TMPro;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 화면에 모두가 같이 쓰는 골드를 보여 준다. 날짜와 시계는 DayCycleHud가 맡는다.
    public class ShopHud : MonoBehaviour
    {
        [SerializeField] private GameState _gameState;
        [SerializeField] private TMP_Text _goldText;

        private void OnEnable()
        {
            _gameState.GoldChanged += ShowGold;
            ShowGold(_gameState.IsReady ? _gameState.Gold : 0);
        }

        private void OnDisable()
        {
            _gameState.GoldChanged -= ShowGold;
        }

        private void ShowGold(int gold)
        {
            _goldText.text = $"{gold:N0} G";
        }
    }
}
