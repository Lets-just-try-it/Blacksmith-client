using System;
using Blacksmith.Core.Adventurers;
using Blacksmith.Core.Inventory;
using Fusion;
using UnityEngine;

namespace Blacksmith.Plaza
{
    // 관리 중인 모험가 목록이다. 호스트만 값을 바꾸고, 클라이언트는 복제된 값을 읽기만 한다.
    public class AdventurerRosterState : NetworkBehaviour
    {
        // [Networked] 배열 크기는 컴파일할 때 정해야 해서 상수로 둔다. 기획상 최대 슬롯 수와 같다.
        public const int MaxCapacity = 8;

        [SerializeField] private AdventurerRosterData _rosterData;

        private ChangeDetector _changes;

        [Networked, Capacity(MaxCapacity)] private NetworkArray<NetworkAdventurer> Adventurers => default;
        [Networked] public int Count { get; private set; }
        [Networked] public int SlotCount { get; private set; }
        [Networked] private int NextAdventurerId { get; set; }

        // 스폰 직후와 목록·슬롯이 바뀔 때 모든 참가자에게 불린다.
        public event Action Changed;

        // 스폰 전에는 [Networked] 값을 읽을 수 없다.
        public bool IsReady => Object != null && Object.IsValid;
        public bool HasFreeSlot => Count < SlotCount;

        public override void Spawned()
        {
            _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
            if (HasStateAuthority)
            {
                SlotCount = _rosterData.InitialSlotCount;
                NextAdventurerId = AdventurerData.NoAdventurerId + 1;
            }

            Changed?.Invoke();
        }

        public override void Render()
        {
            bool hasChanged = false;
            foreach (string _ in _changes.DetectChanges(this))
            {
                hasChanged = true;
            }

            if (hasChanged)
            {
                Changed?.Invoke();
            }
        }

        public AdventurerData Get(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return Adventurers[index].ToData();
        }

        public AdventurerData FindById(int adventurerId)
        {
            int index = IndexOf(adventurerId);
            return index >= 0 ? Adventurers[index].ToData() : null;
        }

        // 호스트 전용. 성공하면 새 AdventurerId를 발급해 adventurer에 채운다.
        public bool TryAdd(AdventurerData adventurer)
        {
            if (!HasStateAuthority || !HasFreeSlot)
            {
                return false;
            }

            adventurer.AdventurerId = NextAdventurerId++;
            Adventurers.Set(Count, NetworkAdventurer.FromData(adventurer));
            Count++;
            return true;
        }

        // 호스트 전용. 모험가의 장비 칸 하나를 바꾼다. 장비를 벗기려면 itemId에 NoEquipment를 넣는다.
        // 칸에 맞는 장비인지는 부르는 쪽(장비 착용 창)이 EquipmentRules로 확인한다.
        public bool TrySetEquipment(int adventurerId, EquipmentSlot slot, int itemId, int grade)
        {
            int index = IndexOf(adventurerId);
            if (!HasStateAuthority || index < 0 || slot == EquipmentSlot.None)
            {
                return false;
            }

            NetworkAdventurer adventurer = Adventurers[index];
            if (itemId == AdventurerData.NoEquipment)
            {
                grade = 0;
            }

            switch (slot)
            {
                case EquipmentSlot.Weapon:
                    adventurer.WeaponId = itemId;
                    adventurer.WeaponGrade = grade;
                    break;
                case EquipmentSlot.Helmet:
                    adventurer.HelmetId = itemId;
                    adventurer.HelmetGrade = grade;
                    break;
                case EquipmentSlot.Armor:
                    adventurer.ArmorId = itemId;
                    adventurer.ArmorGrade = grade;
                    break;
                case EquipmentSlot.Accessory:
                    adventurer.AccessoryId = itemId;
                    adventurer.AccessoryGrade = grade;
                    break;
            }

            Adventurers.Set(index, adventurer);
            return true;
        }

        // 호스트 전용. 전투 보상으로 계산한 레벨과 현재 레벨에서 쌓은 경험치를 그대로 넣는다.
        public bool TrySetLevel(int adventurerId, int level, int experience)
        {
            int index = IndexOf(adventurerId);
            if (!HasStateAuthority || index < 0)
            {
                return false;
            }

            NetworkAdventurer adventurer = Adventurers[index];
            adventurer.Level = level;
            adventurer.Experience = experience;
            Adventurers.Set(index, adventurer);
            return true;
        }

        // 호스트 전용. 범위는 부르는 쪽이 InjuryRules로 맞춘다.
        public bool TrySetInjury(int adventurerId, int injury)
        {
            int index = IndexOf(adventurerId);
            if (!HasStateAuthority || index < 0)
            {
                return false;
            }

            NetworkAdventurer adventurer = Adventurers[index];
            adventurer.Injury = injury;
            Adventurers.Set(index, adventurer);
            return true;
        }

        // 호스트 전용. 사망한 모험가를 관리 목록에서 뺀다. 입은 장비도 함께 사라진다. 뒤의 모험가는 한 칸씩 당긴다.
        public bool TryRemove(int adventurerId)
        {
            int index = IndexOf(adventurerId);
            if (!HasStateAuthority || index < 0)
            {
                return false;
            }

            for (int i = index; i < Count - 1; i++)
            {
                Adventurers.Set(i, Adventurers[i + 1]);
            }

            Count--;
            Adventurers.Set(Count, default);
            return true;
        }

        // 호스트 전용. 대장간 업그레이드가 부른다. 이미 관리 중인 인원보다 줄이지 않는다.
        public void SetSlotCount(int slotCount)
        {
            if (HasStateAuthority)
            {
                SlotCount = Mathf.Clamp(slotCount, Count, MaxCapacity);
            }
        }

        private int IndexOf(int adventurerId)
        {
            for (int i = 0; i < Count; i++)
            {
                if (Adventurers[i].AdventurerId == adventurerId)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
