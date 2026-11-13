using System.Collections.Generic;
using Blacksmith.Core.Adventurers;

namespace Blacksmith.Core.Battle
{
    // 파견 창에서 파티를 꾸리는 규칙이다. (docs/GAME_CONCEPT.md 6.3, 6.5)
    // 파견 창의 표시와 호스트의 파견 확정이 같은 함수를 쓴다.
    public static class DispatchRules
    {
        // 부상수치가 파견 제한 미만이면 부상당했어도 보낼 수 있다. 사망한 모험가는 관리 목록에서 빠진다.
        public static bool CanDispatch(AdventurerData adventurer, InjuryBalance injury)
        {
            return InjuryRules.CanDispatch(adventurer.Injury, injury);
        }

        public static bool CanAddToParty(int partyCount, int maxPartySize)
        {
            return partyCount < maxPartySize;
        }

        // 한 명 이상, 최대 인원 이하면 보낼 수 있다. 몇 명을 보낼지는 플레이어가 정한다.
        public static bool IsValidPartySize(int partyCount, int maxPartySize)
        {
            return partyCount > 0 && partyCount <= maxPartySize;
        }

        // 고른 스테이지를 고를 수 없으면 그 단계의 기본 스테이지(다음 스테이지, 모두 클리어했으면 보스)로 옮긴다.
        // 단계에 스테이지가 없으면 -1이다.
        public static int ClampStage(int clearedStages, int stageCount, int selectedStage)
        {
            if (stageCount <= 0)
            {
                return -1;
            }

            return DungeonProgressRules.CanSelectStage(clearedStages, stageCount, selectedStage)
                ? selectedStage
                : DungeonProgressRules.DefaultStage(clearedStages, stageCount);
        }

        // 열린 단계는 모두 고를 수 있다. 보스까지 클리어한 단계도 다시 고를 수 있다.
        // clearedStagesByTier와 stageCountByTier는 단계 순서다.
        public static bool IsTierSelectable(int tierIndex, IReadOnlyList<int> clearedStagesByTier, IReadOnlyList<int> stageCountByTier)
        {
            if (tierIndex < 0 || tierIndex >= stageCountByTier.Count || stageCountByTier[tierIndex] <= 0)
            {
                return false;
            }

            return tierIndex == 0
                || DungeonProgressRules.IsTierUnlocked(tierIndex, clearedStagesByTier[tierIndex - 1], stageCountByTier[tierIndex - 1]);
        }
    }
}
