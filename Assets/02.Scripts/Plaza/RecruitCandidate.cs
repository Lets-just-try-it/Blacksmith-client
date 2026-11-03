using Blacksmith.Core.Adventurers;
using Blacksmith.Dialogue;
using Fusion;
using TMPro;
using UnityEngine;

namespace Blacksmith.Plaza
{
    // 광장에 서 있는 영입 후보 한 명이다. 호스트가 PlazaRecruitment로 스폰하고, 영입되면 디스폰한다.
    // 말을 걸면 같은 오브젝트의 DialogueSeat이 대화를 열고, 대화에서 "영입하기"를 고르면 영입을 요청한다.
    [RequireComponent(typeof(DialogueSeat))]
    public class RecruitCandidate : NetworkBehaviour, IDialogueChoiceHandler
    {
        // 영입 대화 데이터의 "영입하기" 선택지 ActionId와 같아야 한다.
        public const string RecruitActionId = "Recruit";

        [Tooltip("직업 이름을 찾는 데 쓴다")]
        [SerializeField] private AdventurerGenerationData _generation;
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private DialogueSeat _seat;

        // 호스트에서만 채워진다. 영입 요청은 호스트에서만 처리하기 때문이다.
        private PlazaRecruitment _recruitment;

        [Networked] public NetworkAdventurer Data { get; private set; }

        // 호스트 전용. 스폰 직전(onBeforeSpawned)에 불러 초기 상태를 채운다.
        public void Initialize(PlazaRecruitment recruitment, AdventurerData data)
        {
            _recruitment = recruitment;
            Data = NetworkAdventurer.FromData(data);
        }

        public override void Spawned()
        {
            AdventurerData data = Data.ToData();
            name = $"RecruitCandidate {data.Name}";
            _nameLabel.text = $"{data.Name}\n{AdventurerLabels.GetJobName(_generation, data.JobId)} Lv.{data.Level}";
            _seat.SpeakerName = data.Name;
        }

        // 호스트 전용.
        public void OnDialogueChoice(string actionId, PlayerRef player)
        {
            if (actionId == RecruitActionId && HasStateAuthority && _recruitment != null)
            {
                _recruitment.TryRecruit(this);
            }
        }
    }
}
