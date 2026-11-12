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
