using System;

namespace Blacksmith.Core.Adventurers
{
    public enum AffinityStage
    {
        Stranger,
        Friendly,
        Trusted,
        Sworn,
    }

    // 친밀도 밸런스 수치다. System.Serializable이라 ScriptableObject 필드로도 쓸 수 있다. (docs/GAME_CONCEPT.md 6.6)
    [Serializable]
    public struct AffinityBalance
    {
        public int InitialAffinity;
        public int MaxAffinity;
        // 이 값 이상이면 그 단계다. 아래는 모두 낯섦이다.
        public int FriendlyAffinity;
        public int TrustedAffinity;
        public int SwornAffinity;
        public int TalkGain;
        public int InnDispatchGain;
        public int EquipGain;
        public int HighGradeEquipGain;
        public int HerbGain;
        public int PotionGain;
        public int SurvivedBattleGain;
        // 전투가 끝났을 때 중상 이상(파견 불가 부상)이면 이만큼 줄어든다.
        public int SevereInjuryLoss;
        // 모험가 한 명이 하루에 얻을 수 있는 최대 증가량. 감소에는 적용하지 않는다.
        public int DailyGainLimit;
    }

    // 친밀도 증감과 단계를 정한다. 화면 표시와 호스트 계산이 같은 함수를 쓴다.
    public static class AffinityRules
    {
        public static int Add(int affinity, int amount, AffinityBalance balance)
        {
            return Math.Clamp(affinity + amount, 0, balance.MaxAffinity);
        }

        public static AffinityStage GetStage(int affinity, AffinityBalance balance)
        {
            if (affinity >= balance.SwornAffinity)
            {
                return AffinityStage.Sworn;
            }

            if (affinity >= balance.TrustedAffinity)
            {
                return AffinityStage.Trusted;
            }

            return affinity >= balance.FriendlyAffinity ? AffinityStage.Friendly : AffinityStage.Stranger;
        }

        // 살아 돌아온 모험가의 전투 후 증감이다. 중상 이상이면 줄고, 아니면 함께 싸운 몫만큼 오른다.
        public static int GetBattleChange(int injuryAfterBattle, InjuryBalance injury, AffinityBalance balance)
        {
            return InjuryRules.CanDispatch(injuryAfterBattle, injury) ? balance.SurvivedBattleGain : -balance.SevereInjuryLoss;
        }
    }
}
