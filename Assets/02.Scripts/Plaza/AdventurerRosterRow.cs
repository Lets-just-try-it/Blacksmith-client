using Blacksmith.Core.Adventurers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blacksmith.Plaza
{
    // 관리 목록의 한 칸. 모험가 한 명 또는 빈 슬롯을 보여 준다.
    public class AdventurerRosterRow : MonoBehaviour
    {
        [SerializeField] private Image _portrait;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _statsText;
        [SerializeField] private TMP_Text _detailText;
        [SerializeField] private TMP_Text _conditionText;

        public void Show(AdventurerData adventurer, string jobName, Sprite portrait)
        {
            AdventurerStats stats = AdventurerStatRules.GetCurrentStats(adventurer);
            AdventurerStats growth = adventurer.GrowthStats;
            AdventurerAppearance appearance = adventurer.Appearance;

            _portrait.enabled = true;
            _portrait.sprite = portrait;
            _nameText.text = $"{adventurer.Name}  {jobName} Lv.{adventurer.Level}";
            _statsText.text = $"체력 {stats.MaxHp} (+{growth.MaxHp})   공격 {stats.Attack} (+{growth.Attack})   방어 {stats.Defense} (+{growth.Defense})";
            // 프리셋 번호는 0부터 세지만 화면에는 1부터 보여 준다.
            _detailText.text =
                $"헤어 {appearance.HairPreset + 1} · 눈 {appearance.EyePreset + 1} · 입 {appearance.MouthPreset + 1} · " +
                $"머리색 {appearance.HairColor + 1} · 피부색 {appearance.SkinColor + 1}\n" +
                $"무기 {AdventurerLabels.GetEquipmentName(adventurer.WeaponId)} · " +
                $"투구 {AdventurerLabels.GetEquipmentName(adventurer.HelmetId)} · " +
                $"갑옷 {AdventurerLabels.GetEquipmentName(adventurer.ArmorId)} · " +
                $"스킬 {AdventurerLabels.GetSkillName(adventurer.SkillId)}";
            _conditionText.text = AdventurerLabels.GetConditionName(adventurer.Condition);
        }

        public void ShowEmpty()
        {
            _portrait.enabled = false;
            _nameText.text = "빈 슬롯";
            _statsText.text = string.Empty;
            _detailText.text = string.Empty;
            _conditionText.text = string.Empty;
        }
    }
}
