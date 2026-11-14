using System;
using Blacksmith.Characters;
using Blacksmith.Core.Adventurers;
using Blacksmith.Dialogue;
using Blacksmith.Inventory;
using Fusion;
using TMPro;
using UnityEngine;

namespace Blacksmith.Plaza
{
    // 여관에 머무는 모험가 한 명의 모습이다. 호스트가 InnResidents로 관리 목록을 따라 스폰·디스폰한다.
    // 관리 목록이 바뀌면(장비, 레벨) 호스트가 Data를 갱신하고, 모든 참가자가 이름표와 외형을 다시 그린다.
    // 말을 걸면 같은 오브젝트의 DialogueSeat이 대화를 열고, 호스트는 대화가 시작될 때 InnResidents에 알려 친밀도를 올린다(6.6).
    [RequireComponent(typeof(DialogueSeat))]
    public class InnResident : NetworkBehaviour
    {
        [Tooltip("직업 이름을 찾는 데 쓴다")]
        [SerializeField] private AdventurerGenerationData _generation;
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private AdventurerLook _look;
        [Tooltip("들고 있는 무기의 모델을 찾는 데 쓴다")]
        [SerializeField] private ItemCatalog _catalog;
        [SerializeField] private DialogueSeat _seat;

        private ChangeDetector _changes;

        // 호스트에서만 쓴다.
        private Action<int> _talked;
        private bool _wasBusy;

        [Networked] public NetworkAdventurer Data { get; private set; }

        public int AdventurerId => Data.AdventurerId;

        // 호스트 전용. 스폰 직전(onBeforeSpawned)과 관리 목록이 바뀔 때 부른다.
        public void SetData(AdventurerData data)
        {
            Data = NetworkAdventurer.FromData(data);
        }

        // 호스트 전용. 스폰 직전(onBeforeSpawned)에 불러, 누가 말을 걸기 시작할 때 이 모험가 번호로 talked를 부르게 한다.
        public void SetTalkHandler(Action<int> talked)
        {
            _talked = talked;
        }

        public override void Spawned()
        {
            _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
            Refresh();
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
            {
                return;
            }

            bool isBusy = _seat.IsBusy;
            if (isBusy && !_wasBusy)
            {
                _talked?.Invoke(AdventurerId);
            }

            _wasBusy = isBusy;
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
            _seat.SpeakerName = data.Name;
            _look.Show(data, _catalog);
        }
    }
}
