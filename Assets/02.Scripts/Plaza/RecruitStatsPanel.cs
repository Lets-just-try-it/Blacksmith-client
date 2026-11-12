using Blacksmith.Core.Adventurers;
using Blacksmith.Inventory;
using TMPro;
using UnityEngine;

namespace Blacksmith.Plaza
{
    // 영입 후보와 대화하는 동안 화면 왼쪽에 그 후보의 능력치를 보여 준다. 씬의 UI에 두고 RecruitStatsChannel에 등록한다.
    public class RecruitStatsPanel : MonoBehaviour
    {
        [SerializeField] private RecruitStatsChannel _channel;
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _statsText;
        [SerializeField] private TMP_Text _detailText;
        [Tooltip("직업 이름을 찾는 데 쓴다")]
        [SerializeField] private AdventurerGenerationData _generation;
        [Tooltip("들고 있는 무기 이름을 찾는 데 쓴다")]
        [SerializeField] private ItemCatalog _catalog;

        private void OnEnable()
        {
            _channel.Register(this);
            Hide();
        }

        private void OnDisable()
        {
            _channel.Unregister(this);
        }

        public void Show(AdventurerData adventurer)
        {
            _titleText.text = $"{adventurer.Name}\n{AdventurerLabels.GetJobName(_generation, adventurer.JobId)} Lv.{adventurer.Level}";
            _statsText.text = AdventurerLabels.FormatStats(adventurer, "\n");
            _detailText.text =
                $"무기 {GetWeaponName(adventurer)}\n" +
                $"스킬 {AdventurerLabels.GetSkillName(adventurer.SkillId)}";
            _panel.SetActive(true);
        }

        public void Hide()
        {
            _panel.SetActive(false);
        }

        private string GetWeaponName(AdventurerData adventurer)
        {
            ItemData weapon = adventurer.WeaponId == AdventurerData.NoEquipment ? null : _catalog.Find(adventurer.WeaponId);
            return weapon != null ? weapon.DisplayName : AdventurerLabels.GetEquipmentName(adventurer.WeaponId);
        }
    }
}
