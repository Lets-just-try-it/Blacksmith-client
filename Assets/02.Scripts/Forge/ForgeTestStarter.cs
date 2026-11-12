using System;
using System.Collections.Generic;
using Blacksmith.Core.Adventurers;
using Blacksmith.Core.Inventory;
using Blacksmith.Inventory;
using Blacksmith.Network;
using Blacksmith.Plaza;
using Fusion;
using UnityEngine;

namespace Blacksmith.Forge
{
    // ForgeTest 씬을 혼자 확인하기 위한 진입점이다. 로비를 거치지 않고 참가자가 없는 호스트로 시작한다.
    // NetworkBootstrap의 Game 씬 경로를 ForgeTest로 지정해 둔다.
    // NetworkBootstrap을 비워 두면 호스트를 시작하지 않는다. 로비를 거쳐 들어오는 Game 씬은 그렇게 두고 시험용 재료와 모험가만 넣는다.
    // 재료 수급 방식이 정해지기 전까지(기획 [미정]) 공용 창고에 시험용 재료를 한 번 채워 넣는다.
    // 밤 파견 창을 확인하도록 관리 목록에 직업마다 모험가를 한 명씩 넣는다.
    public class ForgeTestStarter : MonoBehaviour
    {
        [Serializable]
        private struct ItemSupply
        {
            public ItemData Item;
            [Min(1)] public int Count;
            [Tooltip("제작 중간 형태일 때 지금까지 쌓은 정확도 평균(0~100). 재료와 장비는 0")]
            [Range(0, 100)] public int Accuracy;
        }

        [SerializeField] private NetworkBootstrap _bootstrap;
        [SerializeField] private SharedInventoryState _inventory;
        [SerializeField] private ItemData _supplyItem;
        [SerializeField, Min(1)] private int _supplyCount = 10;
        [Tooltip("광석 말고 함께 넣을 재료와 제작 중간 형태. 작업대마다 바로 시험해 볼 수 있게 각 단계의 재료를 넣어 둔다")]
        [SerializeField] private ItemSupply[] _extraSupplies = System.Array.Empty<ItemSupply>();
        [Tooltip("비워 두면 모험가를 넣지 않는다")]
        [SerializeField] private AdventurerRosterState _roster;
        [SerializeField] private AdventurerGenerationData _generation;

        private bool _hasSupplied;
        private bool _hasRecruited;

        private async void Start()
        {
            // 호스트가 시작되면 이 씬을 네트워크 씬으로 다시 불러온다. 그때는 이미 러너가 있으므로 다시 시작하지 않는다.
            if (_bootstrap == null || NetworkRunner.Instances.Count > 0)
            {
                return;
            }

            // 같은 App ID를 쓰는 팀원과 세션이 겹치지 않게 이름을 매번 새로 만든다.
            await _bootstrap.StartHost($"ForgeTest-{Guid.NewGuid():N}");
        }

        // 공용 창고와 관리 목록이 스폰된 뒤 한 번만 채운다. 시험용이라 스폰 이벤트를 따로 두지 않고 확인한다.
        private void Update()
        {
            Supply();
            Recruit();
        }

        private void Supply()
        {
            if (_hasSupplied || _inventory == null || !_inventory.IsReady || !_inventory.HasStateAuthority)
            {
                return;
            }

            _hasSupplied = true;
            _inventory.TryAdd(new ItemStack(_supplyItem.ItemId, 0, _supplyCount));
            foreach (ItemSupply supply in _extraSupplies)
            {
                AddSupply(supply);
            }
        }

        // 재료는 쌓아서 한 번에 넣고, 쌓이지 않는 중간 형태와 장비는 하나씩 넣는다.
        private void AddSupply(ItemSupply supply)
        {
            if (supply.Item == null)
            {
                return;
            }

            if (supply.Item.Kind == ItemKind.Material)
            {
                _inventory.TryAdd(new ItemStack(supply.Item.ItemId, 0, supply.Count));
                return;
            }

            for (int i = 0; i < supply.Count; i++)
            {
                _inventory.TryAdd(new ItemStack(supply.Item.ItemId, 0, 1, supply.Accuracy));
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
