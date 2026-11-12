using System;
using System.Collections.Generic;

namespace Blacksmith.Core.Battle
{
    // 스테이지 안의 무리마다 나올 잡몹을 정한다 (docs/GAME_CONCEPT.md 7.1 "몬스터 무리").
    // 같은 시드면 같은 구성이라, 호스트와 참가자가 같은 전투를 재생한다.
    public static class BattleGroupRules
    {
        // 전투 시뮬레이션·드랍과 같은 시드를 쓰되 다른 난수열이 나오게 섞는 값이다. 밸런스 수치가 아니다.
        private const int SeedSalt = 0x3A7E_51C9;

        // groupCounts[i]마리씩 몹 풀에서 균등하게 뽑아 풀 번호를 돌려준다.
        // 무리 안에서는 사거리가 짧은 몹이 앞에 서도록 사거리 순으로 늘어놓고, 같으면 뽑은 순서를 따른다.
        public static IReadOnlyList<int[]> PickMobs(int seed, IReadOnlyList<int> groupCounts, IReadOnlyList<int> poolRanges)
        {
            var groups = new List<int[]>(groupCounts.Count);
            if (poolRanges.Count == 0)
            {
                foreach (int _ in groupCounts)
                {
                    groups.Add(Array.Empty<int>());
                }

                return groups;
            }

            var random = new Random(seed ^ SeedSalt);
            foreach (int count in groupCounts)
            {
                var picks = new List<int>(Math.Max(0, count));
                for (int i = 0; i < count; i++)
                {
                    picks.Add(random.Next(poolRanges.Count));
                }

                groups.Add(SortByRange(picks, poolRanges));
            }

            return groups;
        }

        // List.Sort는 같은 값의 순서를 지키지 않아 삽입 정렬로 뽑은 순서를 지킨다.
        private static int[] SortByRange(List<int> picks, IReadOnlyList<int> poolRanges)
        {
            int[] sorted = picks.ToArray();
            for (int i = 1; i < sorted.Length; i++)
            {
                int current = sorted[i];
                int j = i - 1;
                while (j >= 0 && poolRanges[sorted[j]] > poolRanges[current])
                {
                    sorted[j + 1] = sorted[j];
                    j--;
                }

                sorted[j + 1] = current;
            }

            return sorted;
        }
    }
}
