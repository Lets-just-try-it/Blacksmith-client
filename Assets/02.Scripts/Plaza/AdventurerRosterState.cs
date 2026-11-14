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

        public AffinityBalance AffinityBalance => _rosterData.Affinity;

        // 호스트 전용. 성공하면 새 AdventurerId를 발급해 adventurer에 채우고, 친밀도를 영입 시 값으로 맞춘다.
        public bool TryAdd(AdventurerData adventurer)
        {
            if (!HasStateAuthority || !HasFreeSlot)
            {
                return false;
            }

            adventurer.AdventurerId = NextAdventurerId++;
            adventurer.Affinity = _rosterData.Affinity.InitialAffinity;
            Adventurers.Set(Count, NetworkAdventurer.FromData(adventurer));
            Count++;
            return true;
        }

        // 호스트 전용. 모험가의 장비 칸 하나를 item(번호·등급·쓴 내구도)으로 바꾼다. 장비를 벗기려면 ItemStack.Empty를 넣는다.
        // 칸에 맞는 장비인지는 부르는 쪽(장비 착용 창)이 EquipmentRules로 확인한다.
        public bool TrySetEquipment(int adventurerId, EquipmentSlot slot, ItemStack item)
        {
            int index = IndexOf(adventurerId);
            if (!HasStateAuthority || index < 0 || slot == EquipmentSlot.None)
            {
                return false;
            }

            NetworkAdventurer adventurer = Adventurers[index];
            int itemId = item.IsEmpty ? AdventurerData.NoEquipment : item.ItemId;
            int grade = item.IsEmpty ? 0 : item.Grade;
            int wear = item.IsEmpty ? 0 : item.Wear;
            switch (slot)
            {
                case EquipmentSlot.Weapon:
                    adventurer.WeaponId = itemId;
                    adventurer.WeaponGrade = grade;
                    adventurer.WeaponWear = wear;
                    break;
                case EquipmentSlot.Helmet:
                    adventurer.HelmetId = itemId;
                    adventurer.HelmetGrade = grade;
                    adventurer.HelmetWear = wear;
                    break;
                case EquipmentSlot.Armor:
                    adventurer.ArmorId = itemId;
                    adventurer.ArmorGrade = grade;
                    adventurer.ArmorWear = wear;
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

        // 호스트 전용. 친밀도를 amount만큼 바꾸고 0~최대로 맞춘다. 줄일 때는 음수를 넣는다.
        public bool TryAddAffinity(int adventurerId, int amount)
        {
            int index = IndexOf(adventurerId);
            if (!HasStateAuthority || index < 0)
            {
                return false;
            }

            NetworkAdventurer adventurer = Adventurers[index];
            adventurer.Affinity = AffinityRules.Add(adventurer.Affinity, amount, _rosterData.Affinity);
            Adventurers.Set(index, adventurer);
            return true;
        }

        // 호스트 전용. 여관에서 말을 걸었을 때 부른다. 그날 처음 대화한 모험가만 친밀도가 오르고, 하루 증가 제한을 지킨다.
        public bool TryTalk(int adventurerId, int day)
        {
            int index = IndexOf(adventurerId);
            if (!HasStateAuthority || index < 0)
            {
                return false;
            }

            NetworkAdventurer adventurer = Adventurers[index];
            if (!AffinityRules.CanGainFromTalk(adventurer.LastTalkDay, day))
            {
                return false;
            }

            adventurer.LastTalkDay = day;
            AddDailyGain(ref adventurer, _rosterData.Affinity.TalkGain, day);
            Adventurers.Set(index, adventurer);
            return true;
        }

        // 호스트 전용. 여관에서 장비를 건네 실제로 입혔을 때 부른다. 하루 한 번만 오르고, 상급 이상이면 더 오른다.
        public bool TryGiftEquipment(int adventurerId, int grade, int day)
        {
            int index = IndexOf(adventurerId);
            if (!HasStateAuthority || index < 0)
            {
                return false;
            }

            NetworkAdventurer adventurer = Adventurers[index];
            if (!AffinityRules.CanGainOncePerDay(adventurer.LastEquipGiftDay, day))
            {
                return false;
            }

            adventurer.LastEquipGiftDay = day;
            AddDailyGain(ref adventurer, AffinityRules.GetEquipGiftGain(grade, _rosterData.Affinity), day);
            Adventurers.Set(index, adventurer);
            return true;
        }

        // 호스트 전용. 여관에서 대화로 파견 파티에 넣었을 때 부른다. 하루 한 번만 오른다.
        public bool TryJoinPartyAtInn(int adventurerId, int day)
        {
            int index = IndexOf(adventurerId);
            if (!HasStateAuthority || index < 0)
            {
                return false;
            }

            NetworkAdventurer adventurer = Adventurers[index];
            if (!AffinityRules.CanGainOncePerDay(adventurer.LastInnDispatchDay, day))
            {
                return false;
            }

            adventurer.LastInnDispatchDay = day;
            AddDailyGain(ref adventurer, _rosterData.Affinity.InnDispatchGain, day);
            Adventurers.Set(index, adventurer);
            return true;
        }

        // 호스트 전용. 여관에서 대화로 파견 파티에서 뺐을 때 부른다. 뺄 때마다 조금 줄어든다.
        public bool TryLeavePartyAtInn(int adventurerId)
        {
            return TryAddAffinity(adventurerId, -_rosterData.Affinity.InnLeavePartyLoss);
        }

        private void AddDailyGain(ref NetworkAdventurer adventurer, int amount, int day)
        {
            if (adventurer.AffinityGainDay != day)
            {
                adventurer.AffinityGainDay = day;
                adventurer.AffinityGainedToday = 0;
            }

            int allowed = AffinityRules.GetAllowedGain(amount, adventurer.AffinityGainedToday, _rosterData.Affinity);
            adventurer.Affinity = AffinityRules.Add(adventurer.Affinity, allowed, _rosterData.Affinity);
            adventurer.AffinityGainedToday += allowed;
        }

        // 호스트 전용. 사망한 모험가를 관리 목록에서 뺀다. 입은 장비도 함께 사라진다. 뒤의 모험가는 한 칸씩 당긴다.
        // 사망 기록은 부르는 쪽이 먼저 FallenAdventurerState에 남긴다.
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
