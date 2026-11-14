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
        // 여관 대화로 파티에서 뺄 때마다 이만큼 줄어든다.
        public int InnLeavePartyLoss;
        public int EquipGain;
        public int HighGradeEquipGain;
        // 이 등급 이상의 장비를 건네면 HighGradeEquipGain을 쓴다(상급 = 4).
        public int HighGradeEquipMinGrade;
        public int HerbGain;
        public int PotionGain;
        public int SurvivedBattleGain;
        // 전투가 끝났을 때 중상 이상(파견 불가 부상)이면 이만큼 줄어든다.
        public int SevereInjuryLoss;
        // 모험가 한 명이 하루에 얻을 수 있는 최대 증가량. 감소에는 적용하지 않는다.
        public int DailyGainLimit;
        // 전투 이벤트. 빠른 회복·스킬 강화는 신뢰 단계부터, 사망 면제는 맹세 단계부터 일어난다.
        public int FastRecoveryChancePercent;
        // 빠른 회복이 일어나면 이번 전투에서 입은 부상의 이 비율(%)만 남는다.
        public int FastRecoveryInjuryPercent;
        // 스킬 강화 확률은 신뢰 단계 시작에서 Min, 친밀도 최대에서 Max이고 그 사이는 비례한다.
        public int SkillBoostMinChancePercent;
        public int SkillBoostMaxChancePercent;
        // 스킬 강화가 일어나면 그 전투 동안 스킬 위력에 더하는 비율(%p)
        public int SkillBoostPowerPercent;
        public int DeathExemptionChancePercent;
    }

    // 모험가 한 명이 이번 전투에서 친밀도 이벤트를 얻을 확률과 효과다. 0%면 그 이벤트는 굴리지 않는다.
    public struct AffinityBattleChances
    {
        public int FastRecoveryPercent;
        public int FastRecoveryInjuryPercent;
        public int SkillBoostPercent;
        public int SkillBoostPowerPercent;
        public int DeathExemptionPercent;
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

        // 하루 증가 제한(DailyGainLimit)을 넘지 않게 실제로 더할 양을 구한다. gainedToday는 그날 이미 얻은 양이다.
        public static int GetAllowedGain(int amount, int gainedToday, AffinityBalance balance)
        {
            return Math.Clamp(amount, 0, Math.Max(0, balance.DailyGainLimit - gainedToday));
        }

        // 여관 대화는 모험가마다 하루 한 번만 친밀도를 올린다. 그 뒤로는 대사만 나온다.
        public static bool CanGainFromTalk(int lastTalkDay, int day)
        {
            return CanGainOncePerDay(lastTalkDay, day);
        }

        // 대화·장비 건네기·여관에서 파티에 넣기는 모험가마다 하루 한 번만 친밀도를 올린다.
        public static bool CanGainOncePerDay(int lastDay, int day)
        {
            return lastDay != day;
        }

        // 여관에서 장비를 건넸을 때의 증가량이다. 상급 이상이면 더 오른다.
        public static int GetEquipGiftGain(int grade, AffinityBalance balance)
        {
            return grade >= balance.HighGradeEquipMinGrade ? balance.HighGradeEquipGain : balance.EquipGain;
        }

        // 친밀도 단계에 따라 전투 이벤트 확률을 정한다. (docs/GAME_CONCEPT.md 6.6)
        public static AffinityBattleChances GetBattleChances(int affinity, AffinityBalance balance)
        {
            AffinityStage stage = GetStage(affinity, balance);
            bool isTrusted = stage == AffinityStage.Trusted || stage == AffinityStage.Sworn;
            return new AffinityBattleChances
            {
                FastRecoveryPercent = isTrusted ? balance.FastRecoveryChancePercent : 0,
                FastRecoveryInjuryPercent = balance.FastRecoveryInjuryPercent,
                SkillBoostPercent = isTrusted ? GetSkillBoostChance(affinity, balance) : 0,
                SkillBoostPowerPercent = balance.SkillBoostPowerPercent,
                DeathExemptionPercent = stage == AffinityStage.Sworn ? balance.DeathExemptionChancePercent : 0,
            };
        }

        // 빠른 회복이 일어났을 때 남는 부상이다(내림).
        public static int ReduceInjury(int injuryGained, int remainingPercent)
        {
            return injuryGained * remainingPercent / 100;
        }

        private static int GetSkillBoostChance(int affinity, AffinityBalance balance)
        {
            int span = balance.MaxAffinity - balance.TrustedAffinity;
            if (span <= 0)
            {
                return balance.SkillBoostMaxChancePercent;
            }

            int progress = Math.Clamp(affinity - balance.TrustedAffinity, 0, span);
            int range = balance.SkillBoostMaxChancePercent - balance.SkillBoostMinChancePercent;
            return balance.SkillBoostMinChancePercent + range * progress / span;
        }

        // 살아 돌아온 모험가의 전투 후 증감이다. 중상 이상이면 줄고, 아니면 함께 싸운 몫만큼 오른다.
        public static int GetBattleChange(int injuryAfterBattle, InjuryBalance injury, AffinityBalance balance)
        {
            return InjuryRules.CanDispatch(injuryAfterBattle, injury) ? balance.SurvivedBattleGain : -balance.SevereInjuryLoss;
        }
    }
}
