using System;
using System.Collections.Generic;
using Blacksmith.Core.Adventurers;
using Blacksmith.Core.Inventory;
using Blacksmith.Network;
using Blacksmith.Plaza;
using Fusion;
using UnityEngine;

namespace Blacksmith.Inventory
{
    // InventoryTest 씬을 혼자 확인하기 위한 진입점이다. 로비를 거치지 않고 참가자가 없는 호스트로 시작하고,
    // 공용 창고에 시험용 물건을 넣는다. NetworkBootstrap의 Game 씬 경로를 InventoryTest로 지정해 둔다.
    // 장비 착용 창을 확인하도록 관리 목록에 직업마다 모험가를 한 명씩 넣는다.
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
        [Tooltip("비워 두면 모험가를 넣지 않는다")]
        [SerializeField] private AdventurerRosterState _roster;
        [SerializeField] private AdventurerGenerationData _generation;

        private bool _hasFilled;
        private bool _hasRecruited;

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

        // 공용 창고와 관리 목록이 스폰된 뒤 한 번만 채운다. 시험용이라 스폰 이벤트를 따로 두지 않고 확인한다.
        private void Update()
        {
            FillInventory();
            Recruit();
        }

        private void FillInventory()
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

        private void Recruit()
        {
            if (_hasRecruited || _roster == null || !_roster.IsReady || !_roster.HasStateAuthority)
            {
                return;
            }

            _hasRecruited = true;
            AdventurerGenerationRules rules = _generation.ToRules();
            IReadOnlyList<AdventurerJobRules> jobs = rules.Jobs;
            for (int i = 0; i < jobs.Count; i++)
            {
                rules.Jobs = new List<AdventurerJobRules> { jobs[i] };
                _roster.TryAdd(AdventurerGenerator.Generate(i + 1, rules));
            }
        }
    }
}
