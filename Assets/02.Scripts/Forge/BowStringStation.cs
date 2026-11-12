using System.Text;
using Blacksmith.Core.Forge;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 시위 걸기 작업대(활 제작의 셋째 단계). 힘줄 시위의 끝을 활 끝에 감고, 활을 휘어 시위 고리를 홈에 건다.
    public class BowStringStation : MiniStation<BowStringGame>
    {
        [SerializeField] private BowStringRulesData _rulesData;

        private readonly StringBuilder _builder = new StringBuilder();
        private BowStringRules _rules;
        private int _listKey = int.MinValue;

        protected override void OnAwake()
        {
            _rules = _rulesData.ToRules();
        }

        protected override BowStringGame CreateGame(int seed, int difficulty)
        {
            _listKey = int.MinValue;
            return new BowStringGame(_rules, seed, difficulty);
        }

        protected override string Objective =>
            "힘줄 시위를 활에 걸자!\n• 1단계 감기: 마우스 왼쪽 버튼을 누른 채 시계 방향으로 원을 그려 힘줄을 활 끝에 감기. 한 바퀴가 한 번\n• 고리 색이 초록이면 알맞게 팽팽하다. 파랑은 느슨하고, 너무 빠르면 힘줄이 미끄러져 그 바퀴는 안 감긴다\n• 2단계 걸기: 활을 휘면서 고리를 미는 두 손 작업이다. 버튼을 누른 채 아래로 끌면 활이 휜다(오른쪽 세로 막대)\n• 휘기를 초록 띠 안에 두면서 옆으로 끌면 고리가 활 끝으로 밀린다. 덜 휘면 안 움직이고, 너무 휘면 고리가 미끄러져 뒤로 밀린다\n• 띠는 천천히 오르내리니 계속 맞춰야 한다. 고리가 활 끝 홈에 닿으면 걸린다 (왼쪽 끝, 오른쪽 끝 두 번)";

        protected override void Refresh(BowStringGame game, MiniPanelView view)
        {
            ((BowStringStage)Stage).Show(game, Time.deltaTime);
            view.SetChip(0, "감기  " + game.WrapsDone + " / " + game.Wraps);
            view.SetChip(1, "미끄러짐  " + (game.Slips + game.HookSlips) + "회", game.Slips + game.HookSlips > 0 ? new Color(0.3f, 0.1f, 0.08f, 0.95f) : (Color?)null);
            view.SetChip(2, "걸기  " + game.Hooked + " / " + BowStringGame.FlexCount);
            RefreshStatus(game, view);
            RefreshList(game, view);
            RefreshScore(game, view);
            RefreshTime(game, view);
        }

        private static void RefreshStatus(BowStringGame game, MiniPanelView view)
        {
            string message;
            Color color = new Color(1f, 0.85f, 0.5f);
            switch (game.Phase)
            {
                case BowStringGame.PhaseKind.Intro:
                    message = "시위 걸기 시작! 먼저 힘줄을 활 끝에 감자";
                    break;
                case BowStringGame.PhaseKind.Wrap:
                    if (!game.Holding)
                    {
                        message = "왼쪽 버튼을 누른 채 시계 방향으로 원을 그리자";
                    }
                    else if (game.Speed > game.SlipSpeed)
                    {
                        message = "너무 빠르다! 힘줄이 미끄러진다";
                        color = new Color(1f, 0.5f, 0.4f);
                    }
                    else if (game.Speed > game.SpeedHi)
                    {
                        message = "조금 빠르다. 천천히";
                        color = new Color(1f, 0.7f, 0.45f);
                    }
                    else if (game.Speed >= game.SpeedLo)
                    {
                        message = "좋아! 이 빠르기로 계속";
                        color = new Color(0.6f, 1f, 0.7f);
                    }
                    else
                    {
                        message = "더 빠르게 돌리자 (시계 방향)";
                    }
                    break;
                case BowStringGame.PhaseKind.Flex:
                    if (!game.Holding)
                    {
                        message = (game.FlexIndex == 0 ? "왼쪽" : "오른쪽") + " 끝 고리를 걸자. 버튼을 누르고 아래로 끌어 활을 휘자";
                    }
                    else if (game.OverFlexed)
                    {
                        message = "너무 휘었다! 고리가 미끄러진다. 조금 올리자";
                        color = new Color(1f, 0.5f, 0.4f);
                    }
                    else if (game.InWindow)
                    {
                        message = "좋아! 이 휘기로 옆으로 끌어 고리를 밀자";
                        color = new Color(0.6f, 1f, 0.7f);
                    }
                    else
                    {
                        message = "더 아래로 끌어 활을 휘자";
                    }
                    break;
                default:
                    message = game.TimedOut ? "시간이 다 됐다!" : "시위를 걸었다!";
                    color = new Color(1f, 0.95f, 0.8f);
                    break;
            }

            view.SetStatus(message, color);
        }

        private void RefreshList(BowStringGame game, MiniPanelView view)
        {
            int key = game.WrapsDone * 1000 + (game.Slips + game.HookSlips) * 100 + game.Hooked * 10 + (int)game.Phase;
            if (key == _listKey)
            {
                return;
            }

            _listKey = key;
            _builder.Clear();
            _builder.Append("<size=130%><color=#FFD973>시위 걸기 기록</color></size>\n");
            _builder.Append("<color=#99FFB3>감기  ").Append(game.WrapsDone).Append(" / ").Append(game.Wraps).Append("</color>\n");
            _builder.Append(game.Slips > 0 ? "<color=#FF8877>" : "<color=#AAAAAA>").Append("미끄러짐  ").Append(game.Slips).Append("</color>\n");
            _builder.Append("<color=#99FFB3>걸기  ").Append(game.Hooked).Append(" / ").Append(BowStringGame.FlexCount).Append("</color>\n");
            view.SetList(_builder.ToString());
        }

        private static void RefreshScore(BowStringGame game, MiniPanelView view)
        {
            if (game.WrapsDone == 0 && game.Hooked == 0)
            {
                view.SetScore("지금까지 점수  -", new Color(1f, 1f, 1f, 0.7f));
                return;
            }

            float value = game.Progress;
            Color color = value >= game.SuccessScore ? new Color(0.5f, 0.92f, 0.45f) : (value >= 0.3f ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.55f, 0.45f));
            view.SetScore("지금까지 점수  " + Mathf.RoundToInt(value * 100f) + "점", color);
        }

        private static void RefreshTime(BowStringGame game, MiniPanelView view)
        {
            if (game.Phase != BowStringGame.PhaseKind.Wrap && game.Phase != BowStringGame.PhaseKind.Flex)
            {
                view.SetTime(false, 0f, string.Empty, Color.white);
                return;
            }

            view.SetTime(true, game.TimeLeft / game.TimeLimit, "남은 시간 " + game.TimeLeft.ToString("0.0"), game.TimeLeft < 8f ? new Color(1f, 0.5f, 0.4f) : new Color(1f, 1f, 1f, 0.7f));
        }

        protected override void HandleEvent(BowStringGame game, MiniEvent miniEvent, MiniStage stage, MiniPanelView view, MiniBoardView board)
        {
            Vector2 position = board.ToLocal(0f, -2.4f);
            switch (miniEvent.Kind)
            {
                case MiniEventKind.PhaseChanged:
                    stage.Play(ForgeProceduralSfx.Tick(), 0.4f, miniEvent.Index == 0 ? 1.2f : 0.9f);
                    if (miniEvent.Index == 1)
                    {
                        view.Popup("감기 끝! 이제 걸자", new Color(1f, 0.9f, 0.4f), position);
                    }
                    break;
                case MiniEventKind.Perfect:
                    stage.Play(ForgeProceduralSfx.Whoosh(), 0.25f, 1.9f);
                    view.Popup("팽팽하게 감겼다", new Color(0.6f, 1f, 0.7f), position);
                    break;
                case MiniEventKind.Hit:
                    stage.Play(ForgeProceduralSfx.Whoosh(), 0.2f, miniEvent.Extra < 0f ? 1.3f : 2.2f);
                    view.Popup(miniEvent.Extra < 0f ? "조금 느슨하다" : "조금 빠르다", new Color(1f, 0.85f, 0.5f), position);
                    break;
                case MiniEventKind.Charge:
                    stage.Play(ForgeProceduralSfx.Creak(), 0.2f, 1.5f);
                    break;
                case MiniEventKind.Good:
                    stage.Play(ForgeProceduralSfx.Twang(), 0.5f, 1.2f);
                    stage.Burst(14);
                    view.Popup("딱 걸렸다!", new Color(0.6f, 1f, 0.7f), position);
                    break;
                case MiniEventKind.Miss:
                    if (game.Phase == BowStringGame.PhaseKind.Wrap)
                    {
                        stage.Play(ForgeProceduralSfx.Fail(), 0.3f, 1.4f);
                        stage.Shake(0.01f, 0.1f);
                        view.Popup("미끄러졌다!", new Color(1f, 0.5f, 0.4f), position);
                    }
                    else
                    {
                        stage.Play(ForgeProceduralSfx.Twang(), 0.3f, 0.8f);
                        view.Popup("고리가 미끄러졌다!", new Color(1f, 0.6f, 0.45f), position);
                    }
                    break;
                case MiniEventKind.Complete:
                    if (miniEvent.Value >= game.SuccessScore)
                    {
                        stage.Play(ForgeProceduralSfx.Success(), 0.7f, 1f);
                        stage.Burst(40);
                    }
                    else
                    {
                        stage.Play(ForgeProceduralSfx.Fail(), 0.7f, 1f);
                    }
                    break;
            }
        }

        protected override string ResultText(BowStringGame game)
        {
            return $"시위 걸기 {Mathf.RoundToInt(game.Accuracy * 100f)}점\n" +
                $"<size=60%>감기 {game.WrapsDone}/{game.Wraps}   미끄러짐 {game.Slips}회   걸기 {game.Hooked}/{BowStringGame.FlexCount}</size>";
        }
    }
}
