using System;
using Blacksmith.Core.Inventory;
using Blacksmith.Inventory;
using Blacksmith.Network;
using Fusion;
using UnityEngine;

namespace Blacksmith.Forge
{
    // ForgeTest 씬을 혼자 확인하기 위한 진입점이다. 로비를 거치지 않고 참가자가 없는 호스트로 시작한다.
    // NetworkBootstrap의 Game 씬 경로를 ForgeTest로 지정해 둔다.
    // 재료 수급 방식이 정해지기 전까지(기획 [미정]) 공용 창고에 시험용 재료를 한 번 채워 넣는다.
    public class ForgeTestStarter : MonoBehaviour
    {
        [SerializeField] private NetworkBootstrap _bootstrap;
        [SerializeField] private SharedInventoryState _inventory;
        [SerializeField] private ItemData _supplyItem;
        [SerializeField, Min(1)] private int _supplyCount = 10;

        private bool _hasSupplied;

        private async void Start()
        {
            // 호스트가 시작되면 이 씬을 네트워크 씬으로 다시 불러온다. 그때는 이미 러너가 있으므로 다시 시작하지 않는다.
            if (NetworkRunner.Instances.Count > 0)
            {
                return;
            }

            // 같은 App ID를 쓰는 팀원과 세션이 겹치지 않게 이름을 매번 새로 만든다.
            await _bootstrap.StartHost($"ForgeTest-{Guid.NewGuid():N}");
        }

        // 공용 창고가 스폰된 뒤 한 번만 채운다.
        private void Update()
        {
            if (_hasSupplied || _inventory == null || !_inventory.IsReady || !_inventory.HasStateAuthority)
            {
                return;
            }

            _hasSupplied = true;
            _inventory.TryAdd(new ItemStack(_supplyItem.ItemId, 0, _supplyCount));
        }
    }
}
