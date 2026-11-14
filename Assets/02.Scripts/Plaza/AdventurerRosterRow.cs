using System;
using Blacksmith.Core.Adventurers;
using Blacksmith.Core.Characters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blacksmith.Plaza
{
    // 관리 목록의 한 칸. 모험가 한 명 또는 빈 슬롯을 보여 준다.
    // 파견 파티에 든 모험가는 표시와 함께 "빼기" 버튼을 보여 준다.
    public class AdventurerRosterRow : MonoBehaviour
    {
        [SerializeField] private Image _portrait;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _statsText;
        [SerializeField] private TMP_Text _detailText;
        [SerializeField] private TMP_Text _conditionText;
        [Tooltip("파견 파티에서 빼는 버튼. 파티에 든 모험가에게만 보인다")]
        [SerializeField] private Button _leavePartyButton;

        private Action _leaveParty;

        private void Awake()
        {
            _leavePartyButton.onClick.AddListener(OnLeavePartyClicked);
            _leavePartyButton.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _leavePartyButton.onClick.RemoveListener(OnLeavePartyClicked);
        }

        // leaveParty: 파티에 들었으면 "빼기"를 눌렀을 때 부른다. 파티에 없으면 null이다.
        public void Show(AdventurerData adventurer, string jobName, Sprite portrait, string affinityText, Action leaveParty)
        {
            HumanoidAppearance appearance = adventurer.Appearance;

            _portrait.enabled = true;
            _portrait.sprite = portrait;
            string partyText = leaveParty != null ? "  <color=#E0B850>[파견 파티]</color>" : string.Empty;
            _nameText.text = $"{adventurer.Name}  {jobName} Lv.{adventurer.Level}  {affinityText}{partyText}";
            _statsText.text = AdventurerLabels.FormatStats(adventurer, "   ");
            // 프리셋 번호는 0부터 세지만 화면에는 1부터 보여 준다.
            _detailText.text =
                $"헤어 {appearance.HairStyle + 1} · 눈 {appearance.EyeStyle + 1} · 입 {appearance.MouthStyle + 1} · " +
                $"머리색 {appearance.HairColor + 1} · 피부색 {appearance.SkinColor + 1} · 의상 {appearance.Outfit + 1}\n" +
                $"무기 {AdventurerLabels.GetEquipmentName(adventurer.WeaponId)} · " +
                $"투구 {AdventurerLabels.GetEquipmentName(adventurer.HelmetId)} · " +
                $"갑옷 {AdventurerLabels.GetEquipmentName(adventurer.ArmorId)} · " +
                $"스킬 {AdventurerLabels.GetSkillName(adventurer.SkillId)}";
            _conditionText.text = AdventurerLabels.FormatCondition(adventurer);
            SetLeaveParty(leaveParty);
        }

        public void ShowEmpty()
        {
            _portrait.enabled = false;
            _nameText.text = "빈 슬롯";
            _statsText.text = string.Empty;
            _detailText.text = string.Empty;
            _conditionText.text = string.Empty;
            SetLeaveParty(null);
        }

        private void SetLeaveParty(Action leaveParty)
        {
            _leaveParty = leaveParty;
            _leavePartyButton.gameObject.SetActive(leaveParty != null);
        }

        private void OnLeavePartyClicked()
        {
            _leaveParty?.Invoke();
        }
    }
}
