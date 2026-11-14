using Blacksmith.Core.Adventurers;

namespace Blacksmith.Plaza
{
    // 모험가 정보를 화면 문구로 바꾼다. 광장 NPC 이름표, 영입 대화의 능력치 창, 관리 목록이 같은 문구를 쓴다.
    public static class AdventurerLabels
    {
        public static string GetJobName(AdventurerGenerationData generation, int jobId)
        {
            AdventurerJobData job = generation != null ? generation.FindJob(jobId) : null;
            return job != null ? job.DisplayName : $"직업 {jobId}";
        }

        // 현재 레벨의 능력치와 레벨당 성장치. separator로 항목 사이를 나눈다.
        public static string FormatStats(AdventurerData adventurer, string separator)
        {
            AdventurerStats stats = AdventurerStatRules.GetCurrentStats(adventurer);
            AdventurerStats growth = adventurer.GrowthStats;
            return $"체력 {stats.MaxHp} (+{growth.MaxHp}){separator}" +
                   $"공격 {stats.Attack} (+{growth.Attack}){separator}" +
                   $"방어 {stats.Defense} (+{growth.Defense})";
        }

        public static string GetConditionName(AdventurerCondition condition)
        {
            switch (condition)
            {
                case AdventurerCondition.Healthy:
                    return "건강";
                case AdventurerCondition.Injured:
                    return "부상";
                case AdventurerCondition.Dead:
                    return "사망";
                default:
                    return condition.ToString();
            }
        }

        // 부상당했으면 부상수치를 함께 적는다. 예: "부상 40"
        public static string FormatCondition(AdventurerData adventurer)
        {
            string name = GetConditionName(adventurer.Condition);
            return adventurer.Condition == AdventurerCondition.Injured ? $"{name} {adventurer.Injury}" : name;
        }

        // 예: "친밀도 15 (낯섦)"
        public static string FormatAffinity(int affinity, AffinityBalance balance)
        {
            return $"친밀도 {affinity} ({GetAffinityStageName(AffinityRules.GetStage(affinity, balance))})";
        }

        public static string GetAffinityStageName(AffinityStage stage)
        {
            switch (stage)
            {
                case AffinityStage.Stranger:
                    return "낯섦";
                case AffinityStage.Friendly:
                    return "친근";
                case AffinityStage.Trusted:
                    return "신뢰";
                case AffinityStage.Sworn:
                    return "맹세";
                default:
                    return stage.ToString();
            }
        }

        public static string GetEquipmentName(int equipmentId)
        {
            return equipmentId == AdventurerData.NoEquipment ? "없음" : $"#{equipmentId}";
        }

        public static string GetSkillName(int skillId)
        {
            return skillId == AdventurerData.NoSkill ? "없음" : $"#{skillId}";
        }
    }
}
