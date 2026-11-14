using Blacksmith.Characters;
using Blacksmith.Core.Adventurers;
using Blacksmith.Inventory;
using Fusion;
using TMPro;
using UnityEngine;

namespace Blacksmith.Plaza
{
    // 여관에 머무는 모험가 한 명의 모습이다. 호스트가 InnResidents로 관리 목록을 따라 스폰·디스폰한다.
    // 관리 목록이 바뀌면(장비, 레벨) 호스트가 Data를 갱신하고, 모든 참가자가 이름표와 외형을 다시 그린다.
    public class InnResident : NetworkBehaviour
    {
        [Tooltip("직업 이름을 찾는 데 쓴다")]
        [SerializeField] private AdventurerGenerationData _generation;
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private AdventurerLook _look;
        [Tooltip("들고 있는 무기의 모델을 찾는 데 쓴다")]
        [SerializeField] private ItemCatalog _catalog;

        private ChangeDetector _changes;

        [Networked] public NetworkAdventurer Data { get; private set; }

        public int AdventurerId => Data.AdventurerId;

        // 호스트 전용. 스폰 직전(onBeforeSpawned)과 관리 목록이 바뀔 때 부른다.
        public void SetData(AdventurerData data)
        {
            Data = NetworkAdventurer.FromData(data);
        }

        public override void Spawned()
        {
            _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
            Refresh();
        }

        public override void Render()
        {
            foreach (string change in _changes.DetectChanges(this))
            {
                if (change == nameof(Data))
                {
                    Refresh();
                }
            }
        }

        private void Refresh()
        {
            AdventurerData data = Data.ToData();
            name = $"InnResident {data.Name}";
            _nameLabel.text = $"{data.Name}\n{AdventurerLabels.GetJobName(_generation, data.JobId)} Lv.{data.Level}";
            _look.Show(data, _catalog);
        }
    }
}
