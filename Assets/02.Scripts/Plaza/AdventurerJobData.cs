using Blacksmith.Core;
using Blacksmith.Core.Adventurers;
using Blacksmith.Core.Inventory;
using Blacksmith.Inventory;
using UnityEngine;

namespace Blacksmith.Plaza
{
    // 직업 하나의 정의다. 직업을 늘릴 때는 코드를 고치지 않고 이 에셋을 추가해 AdventurerGeneration에 등록한다.
    // [미정] 직업별 공격 방식(근접 광역, 원거리 단일 등)은 전투 담당이 정해 추가한다.
    [CreateAssetMenu(fileName = "Job", menuName = "Blacksmith/Adventurer/Job")]
    public class AdventurerJobData : ScriptableObject
    {
        [Tooltip("저장·동기화에 쓰는 번호. 한 번 정하면 바꾸지 않는다. 0은 쓰지 않는다.")]
        [SerializeField, Min(1)] private int _jobId = 1;
        [SerializeField] private string _displayName;
        [Tooltip("1레벨에 타고나는 체력·공격력·방어력 범위")]
        [SerializeField] private AdventurerStatRange _baseStats;
        [Tooltip("레벨이 하나 오를 때마다 늘어나는 값의 범위. 모험가마다 이 안에서 타고난다.")]
        [SerializeField] private AdventurerStatRange _growthStats;
        [Tooltip("생성할 때 하나를 고르는 스킬 번호 후보. 비우면 스킬 없음")]
        [SerializeField] private int[] _skillIds = new int[0];
        [Tooltip("이 직업이 무기 칸에 착용할 수 있는 무기 종류")]
        [SerializeField] private WeaponType _weaponType;
        [Tooltip("영입 후보가 처음부터 들고 있는 무기. 이 직업의 무기 종류여야 한다")]
        [SerializeField] private ItemData _defaultWeapon;
        [Tooltip("기본무기의 등급")]
        [SerializeField, Min(GradeRule.MinGrade)] private int _defaultWeaponGrade = GradeRule.MinGrade;

        public int JobId => _jobId;
        public string DisplayName => _displayName;
        public WeaponType WeaponType => _weaponType;
        // 영입할 때 들고 있고, 무기를 벗기거나 무기가 부서지면 돌아가는 무기다.
        public ItemStack DefaultWeapon => _defaultWeapon != null ? new ItemStack(_defaultWeapon.ItemId, _defaultWeaponGrade, 1) : ItemStack.Empty;

        public AdventurerJobRules ToRules()
        {
            return new AdventurerJobRules
            {
                JobId = _jobId,
                BaseStats = _baseStats,
                GrowthStats = _growthStats,
                SkillIds = _skillIds,
                DefaultWeaponId = _defaultWeapon != null ? _defaultWeapon.ItemId : AdventurerData.NoEquipment,
                DefaultWeaponGrade = _defaultWeaponGrade,
            };
        }

        private void OnValidate()
        {
            WarnIfInvalid(_baseStats, "기본값");
            WarnIfInvalid(_growthStats, "성장값");
            if (_defaultWeapon == null)
            {
                Debug.LogWarning("기본무기가 없다. 모험가는 무기를 든 채로 생성되어야 한다.", this);
            }
            else if (_defaultWeapon.WeaponType != _weaponType)
            {
                Debug.LogWarning($"기본무기({_defaultWeapon.DisplayName})가 이 직업의 무기 종류가 아니다.", this);
            }
        }

        private void WarnIfInvalid(AdventurerStatRange range, string label)
        {
            if (range.MaxHp.Min > range.MaxHp.Max || range.Attack.Min > range.Attack.Max || range.Defense.Min > range.Defense.Max)
            {
                Debug.LogWarning($"{label} 범위의 최솟값이 최댓값보다 크다.", this);
            }
        }
    }
}
