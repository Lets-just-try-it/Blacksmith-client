using System;
using System.Collections.Generic;
using Blacksmith.Core.Battle;
using UnityEngine;

namespace Blacksmith.Battle
{
    public class BattleHud : MonoBehaviour
    {
        [SerializeField] private BattlePlayer _player;
        [SerializeField] private Transform _slotRoot;
        [SerializeField] private BattleHudSlot _slotPrefab;

        private readonly List<BattleHudSlot> _slots = new List<BattleHudSlot>();
        private Func<int, Sprite> _getPortrait;

        public void SetPortraitProvider(Func<int, Sprite> getPortrait)
        {
            _getPortrait = getPortrait;
        }

        private void OnEnable()
        {
            _player.UnitSpawned += HandleUnitSpawned;
            _player.TickAdvanced += RefreshSlots;
        }

        private void OnDisable()
        {
            _player.UnitSpawned -= HandleUnitSpawned;
            _player.TickAdvanced -= RefreshSlots;
        }

        public void Clear()
        {
            foreach (BattleHudSlot slot in _slots)
            {
                Destroy(slot.gameObject);
            }

            _slots.Clear();
        }

        private void HandleUnitSpawned(BattleUnitView view)
        {
            if (view.Unit.Team != BattleTeam.Adventurer)
            {
                return;
            }

            BattleHudSlot slot = Instantiate(_slotPrefab, _slotRoot);
            slot.Bind(view.Unit, _getPortrait?.Invoke(view.Unit.SourceId));
            _slots.Add(slot);
        }

        private void RefreshSlots()
        {
            foreach (BattleHudSlot slot in _slots)
            {
                slot.Refresh();
            }
        }
    }
}
