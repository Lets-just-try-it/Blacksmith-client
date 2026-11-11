using System;

namespace Blacksmith.Core.Forge
{
    // 밸런스 수치는 ScriptableObject에서 읽어 넘긴다.
    public sealed class SmeltingRules
    {
        // ① 쪼개기
        // 첫 균열이 닿을 때까지 걸리는 시간
        public float CrackLead { get; set; }
        // 고리가 나타나서 균열에 닿기까지 걸리는 시간
        public float Approach { get; set; }
        // 균열과 클릭 시간 차이가 이 안이면 완벽, 좋음, 약함이다. 난이도의 WindowScale이 곱해진다.
        public float PerfectWindow { get; set; }
        public float GoodWindow { get; set; }
        public float WeakWindow { get; set; }
        // 저주 균열은 이 시간이 지나면 넘긴 것으로 친다.
        public float DecoyWindow { get; set; }
        // 아무것도 없는 곳을 누를 때마다 깎이는 정확도
        public float StrayPenalty { get; set; }
        public float DecoyPenalty { get; set; }
        // 이 번호 이후의 균열부터 저주 균열이 나올 수 있다.
        public int DecoyStartIndex { get; set; }
        // 모든 균열이 끝난 뒤 원석이 갈라져 풀무질로 넘어갈 때까지의 시간
        public float SplitDelay { get; set; }
        // 균열이 놓이는 위치: 이전 균열에서 떨어지는 거리, 원석 반지름 안쪽 한계, 같은 시간대 균열과의 최소 간격
        public float CrackFirstDistance { get; set; }
        public float CrackMinDistance { get; set; }
        public float CrackMaxDistance { get; set; }
        public float CrackRadiusLimit { get; set; }
        public float CrackMinSeparation { get; set; }

        // ② 풀무질
        public float BellowsMax { get; set; }
        // 지시선 반폭
        public float CursorHalf { get; set; }
        public float RiseAccel { get; set; }
        public float FallAccel { get; set; }
        public float MaxRise { get; set; }
        public float MaxFall { get; set; }
        public float HeatStart { get; set; }
        // 불꽃 띠가 한 곳에 머무는 시간 = ZoneHoldMin + (0~ZoneHoldRange 사이 값)
        public float ZoneHoldMin { get; set; }
        public float ZoneHoldRange { get; set; }
        // 다 달궈진 뒤 붓기로 넘어갈 때까지의 시간
        public float MoltenHold { get; set; }
        // 제한시간 안에 못 달궜을 때 쇳물 묽기 = TimeoutFluidityBase + TimeoutFluidityHeat * 열
        public float TimeoutFluidityBase { get; set; }
        public float TimeoutFluidityHeat { get; set; }
        // 쇳물 묽기 = 1 - FluidityLoss * (걸린 시간 - FluidityGrace). FluidityMin 아래로는 내려가지 않는다.
        public float FluidityLoss { get; set; }
        public float FluidityGrace { get; set; }
        public float FluidityMin { get; set; }

        // ③ 붓기
        public float PourMax { get; set; }
        public float FlowMax { get; set; }
        public float TiltDead { get; set; }
        // 도가니가 비어 갈수록 흐르기 시작하는 기울기가 이만큼까지 늘어난다.
        public float TiltEmptyBonus { get; set; }
        // 틀 가장자리 바깥으로 이 거리까지는 일부만 틀에 들어간다.
        public float MoldEdge { get; set; }
        public float SpoutLimit { get; set; }
        public float SpoutStart { get; set; }
        public float MoldMin { get; set; }
        public float MoldMax { get; set; }
        // 기울기를 100% 늘렸을 때 쇳물 줄기가 날아가는 거리
        public float Reach { get; set; }
        public float OverflowEnd { get; set; }
        public float ColdLimit { get; set; }
        public float HeatLoss { get; set; }
        // 쇳물 양 = ReserveBase + ReserveCrackBonus * 쪼개기 점수
        public float ReserveBase { get; set; }
        public float ReserveCrackBonus { get; set; }
        // 찌꺼기 양 = DrossBase + DrossCrackPenalty * (1 - 쪼개기 점수)
        public float DrossBase { get; set; }
        public float DrossCrackPenalty { get; set; }
        // 흘린 양, 틀에 들어간 찌꺼기 양이 이 값이면 각각 점수가 0이 된다.
        public float SpillZero { get; set; }
        public float SlagZero { get; set; }
        public float EndDelay { get; set; }

        // 정확도 = 쪼개기 * CrackWeight + 묽기 * FluidityWeight + 붓기 * PourWeight
        public float CrackWeight { get; set; }
        public float FluidityWeight { get; set; }
        public float PourWeight { get; set; }

        public SmeltingDifficulty[] Difficulties { get; set; }

        // 정확도에 따라 나오는 주괴 수. MinAccuracy가 낮은 순서이고 첫 칸의 MinAccuracy는 0이다.
        public SmeltingYieldTier[] YieldTiers { get; set; }

        // 정확도(0~1)로 광석 하나에서 나오는 주괴 수를 정한다.
        public int IngotCount(float accuracy)
        {
            int count = 1;
            foreach (SmeltingYieldTier tier in YieldTiers)
            {
                if (accuracy >= tier.MinAccuracy)
                {
                    count = tier.IngotCount;
                }
            }
            return count;
        }

        public int MinIngots => IngotCount(0f);
        public int MaxIngots => IngotCount(1f);
    }

    // 정확도가 MinAccuracy 이상일 때 나오는 주괴 수. 주괴에는 등급이 없고, 정확도는 이 수만 정한다.
    [Serializable]
    public struct SmeltingYieldTier
    {
        public float MinAccuracy;
        public int IngotCount;
    }

    // 광석 등급 하나의 수치. 귀한 광석일수록 판정이 좁고 쇳물이 빨리 식는다.
    [Serializable]
    public struct SmeltingDifficulty
    {
        // 판정 시간 폭에 곱한다. 작을수록 엄격하다.
        public float WindowScale;
        // 균열 클릭 판정 반지름
        public float HitRadius;
        // 균열 사이 간격에 곱한다.
        public float GapScale;
        public int CrackCount;
        // 저주 균열 확률과, 저주 균열 사이에 둘 최소 균열 수
        public float DecoyChance;
        public int DecoyGap;
        // 불꽃 띠 반폭과 이동 속도
        public float FlameHalfWidth;
        public float ZoneSpeed;
        // 불꽃 띠가 한 곳에 머무는 시간을 나누는 값. 클수록 자주 방향을 튼다.
        public float ZoneHoldDivisor;
        // 띠 안에서 열이 오르는 속도, 밖에서 식는 속도
        public float HeatGain;
        public float HeatDrain;
        // 틀 입구 반폭
        public float MoldHalfWidth;
        // 틀 눈금의 허용 범위와, 벗어났을 때 점수가 0이 되는 거리
        public float FillLow;
        public float FillHigh;
        public float FillTolerance;
        public float FlowScale;
        // 붓는 동안 쇳물이 식는 속도에 곱한다.
        public float HeatLossScale;
    }
}
