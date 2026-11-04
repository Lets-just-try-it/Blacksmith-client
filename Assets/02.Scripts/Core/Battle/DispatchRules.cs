using System.Collections.Generic;
using Blacksmith.Core.Adventurers;

namespace Blacksmith.Core.Battle
{
    // 파견 창에서 파티를 꾸리는 규칙이다. (docs/GAME_CONCEPT.md 6.3, 6.5)
    // 파견 창의 표시와 호스트의 파견 확정이 같은 함수를 쓴다.
    public static class DispatchRules
    {
        // 부상당한 모험가는 파견할 수 없다. 사망한 모험가는 관리 목록에서 빠지지만 혹시 남아 있어도 고르지 않는다.
        public static bool CanDispatch(AdventurerCondition condition)
        {
            return condition == AdventurerCondition.Healthy;
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

        // 고른 스테이지가 고를 수 있는 범위를 벗어났으면 가장 가까운 스테이지로 옮긴다.
        // 단계를 모두 클리어해 고를 스테이지가 없으면 -1이다.
        public static int ClampStage(int clearedStages, int stageCount, int selectedStage)
        {
            int first = DungeonProgressRules.FirstStageToFight(clearedStages);
            if (first >= stageCount)
            {
                return -1;
            }

            if (selectedStage < first)
            {
                return first;
            }

            return selectedStage >= stageCount ? stageCount - 1 : selectedStage;
        }

        // 고를 수 있는 단계인지. clearedStagesByTier와 stageCountByTier는 단계 순서다.
        public static bool IsTierSelectable(int tierIndex, IReadOnlyList<int> clearedStagesByTier, IReadOnlyList<int> stageCountByTier)
        {
            if (tierIndex < 0 || tierIndex >= stageCountByTier.Count)
            {
                return false;
            }

            bool isUnlocked = tierIndex == 0
                || DungeonProgressRules.IsTierUnlocked(tierIndex, clearedStagesByTier[tierIndex - 1], stageCountByTier[tierIndex - 1]);
            return isUnlocked && !DungeonProgressRules.IsTierCleared(clearedStagesByTier[tierIndex], stageCountByTier[tierIndex]);
        }
    }
}
