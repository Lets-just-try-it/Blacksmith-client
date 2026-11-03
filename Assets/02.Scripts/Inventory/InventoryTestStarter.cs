using System;
using System.Collections.Generic;
using Blacksmith.Core.Inventory;
using Blacksmith.Network;
using Fusion;
using UnityEngine;

namespace Blacksmith.Inventory
{
    // InventoryTest 씬을 혼자 확인하기 위한 진입점이다. 로비를 거치지 않고 참가자가 없는 호스트로 시작하고,
    // 공용 창고에 시험용 물건을 넣는다. NetworkBootstrap의 Game 씬 경로를 InventoryTest로 지정해 둔다.
    public class InventoryTestStarter : MonoBehaviour
    {
        [Serializable]
        private struct TestItem
        {
            public ItemData Item;
            [Tooltip("등급이 없는 물건은 0")]
            public int Grade;
            [Min(1)] public int Count;
        }

        [SerializeField] private NetworkBootstrap _bootstrap;
        [SerializeField] private SharedInventoryState _inventory;
        [SerializeField] private List<TestItem> _testItems = new List<TestItem>();

        private bool _hasFilled;

        private async void Start()
        {
            // 호스트가 시작되면 이 씬을 네트워크 씬으로 다시 불러온다. 그때는 이미 러너가 있으므로 다시 시작하지 않는다.
            if (NetworkRunner.Instances.Count > 0)
            {
                return;
            }

            // 같은 App ID를 쓰는 팀원과 세션이 겹치지 않게 이름을 매번 새로 만든다.
            await _bootstrap.StartHost($"InventoryTest-{Guid.NewGuid():N}");
        }

        // 공용 창고가 스폰된 뒤 한 번만 채운다. 시험용이라 스폰 이벤트를 따로 두지 않고 확인한다.
        private void Update()
        {
            if (_hasFilled || !_inventory.IsReady || !_inventory.HasStateAuthority)
            {
                return;
            }

            _hasFilled = true;
            foreach (TestItem testItem in _testItems)
            {
                if (testItem.Item != null)
                {
                    _inventory.TryAdd(new ItemStack(testItem.Item.ItemId, testItem.Grade, testItem.Count));
                }
            }
        }
    }
}
