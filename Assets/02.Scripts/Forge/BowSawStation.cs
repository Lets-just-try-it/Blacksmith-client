using System.Text;
using Blacksmith.Core.Forge;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 활 깎기 작업대(활 제작의 첫 단계). 제한시간 안에 참나무 통나무를 최대한 많이 톱질한다. 마우스 버튼을 누른 채 위아래로 움직이면 톱질이 되고, 톱밥·톱날 색·톱 소리로 힘을 알려 준다.
    public class BowSawStation : MiniStation<BowSawGame>
    {
        [SerializeField] private BowSawRulesData _rulesData;

        private readonly StringBuilder _builder = new StringBuilder();
        private BowSawRules _rules;
        private int _listKey = int.MinValue;

        protected override void OnAwake()
        {
            _rules = _rulesData.ToRules();
        }

        protected override BowSawGame CreateGame(int seed, int difficulty)
        {
            _listKey = int.MinValue;
            return new BowSawGame(_rules, difficulty);
        }

        protected override string Objective =>
            "제한시간 안에 통나무를 최대한 많이 켜자!\n• 마우스 왼쪽 버튼을 누른 채 위아래로 크게 왔다갔다 하면 톱질 (위로 밀고, 아래로 당기기)\n• 힘차게 하면 톱밥이 쏟아지며 잘 켜진다. 너무 느리면 조금밖에 안 켜진다\n• 너무 세게 하면 톱날이 달아올라 불똥이 튀다가 톱이 낀다";

        protected override void Refresh(BowSawGame game, MiniPanelView view)
        {
            ((BowSawStage)Stage).Show(game, Time.deltaTime);
            view.SetChip(0, "켠 통나무  " + game.LogsCut + " / " + game.LogsTarget);
            view.SetChip(1, "톱 낌  " + game.Binds + "회", game.Binds > 0 ? new Color(0.3f, 0.1f, 0.08f, 0.95f) : (Color?)null);
            view.SetChip(2, "톱질  " + game.Strokes + "번");
            RefreshStatus(game, view);
            RefreshList(game, view);
            RefreshScore(game, view);
            RefreshTime(game, view);
        }

        private static void RefreshStatus(BowSawGame game, MiniPanelView view)
        {
            string message;
            Color color = new Color(1f, 0.85f, 0.5f);
            switch (game.Phase)
            {
                case BowSawGame.PhaseKind.Intro:
                    message = "활 깎기 시작! 곧 통나무가 나온다";
                    break;
                case BowSawGame.PhaseKind.Play:
                    if (game.BindLeft > 0f)
                    {
                        message = "톱이 끼었다! 잠깐 기다리자";
                        color = new Color(1f, 0.5f, 0.4f);
                    }
                    else if (!game.Holding)
                    {
                        message = "마우스 버튼을 누른 채 위아래로 톱질하자";
                    }
                    else if (game.Speed > game.ZoneHi)
                    {
                        message = "너무 세다! 조금 살살";
                        color = new Color(1f, 0.6f, 0.4f);
                    }
                    else if (game.Speed >= game.ZoneLo)
                    {
                        message = "좋아! 이 힘으로 계속";
                        color = new Color(0.6f, 1f, 0.7f);
                    }
                    else
                    {
                        message = "더 힘차게 움직이자";
                    }
                    break;
                default:
                    message = game.TimedOut ? "시간이 다 됐다!" : "끝!";
                    color = new Color(1f, 0.95f, 0.8f);
                    break;
            }

            view.SetStatus(message, color);
        }

        private void RefreshList(BowSawGame game, MiniPanelView view)
        {
            int key = game.Strokes * 1000 + game.Binds * 100 + game.LogsCut;
            if (key == _listKey)
            {
                return;
            }

            _listKey = key;
            _builder.Clear();
            _builder.Append("<size=130%><color=#FFD973>활 깎기 기록</color></size>\n");
            _builder.Append("<color=#99FFB3>켠 통나무  ").Append(game.LogsCut).Append(" / ").Append(game.LogsTarget).Append("</color>\n");
            _builder.Append("<color=#AAAAAA>톱질  ").Append(game.Strokes).Append("번</color>\n");
            _builder.Append(game.Binds > 0 ? "<color=#FF8877>" : "<color=#AAAAAA>").Append("톱 낌  ").Append(game.Binds).Append("</color>\n");
            view.SetList(_builder.ToString());
        }

        private static void RefreshScore(BowSawGame game, MiniPanelView view)
        {
            float value = game.Progress;
            if (game.Strokes == 0)
            {
                view.SetScore("지금까지 점수  -", new Color(1f, 1f, 1f, 0.7f));
                return;
            }

            Color color = value >= game.SuccessScore ? new Color(0.5f, 0.92f, 0.45f) : (value >= 0.3f ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.55f, 0.45f));
            view.SetScore("지금까지 점수  " + Mathf.RoundToInt(value * 100f) + "점", color);
        }

        private static void RefreshTime(BowSawGame game, MiniPanelView view)
        {
            if (game.Phase != BowSawGame.PhaseKind.Play)
            {
                view.SetTime(false, 0f, string.Empty, Color.white);
                return;
            }

            view.SetTime(true, game.TimeLeft / game.TimeLimit, "남은 시간 " + game.TimeLeft.ToString("0.0"), game.TimeLeft < 10f ? new Color(1f, 0.5f, 0.4f) : new Color(1f, 1f, 1f, 0.7f));
        }

        protected override void HandleEvent(BowSawGame game, MiniEvent miniEvent, MiniStage stage, MiniPanelView view, MiniBoardView board)
        {
            Vector2 position = board.ToLocal(0f, -2.6f);
            switch (miniEvent.Kind)
            {
                case MiniEventKind.PhaseChanged:
                    stage.Play(ForgeProceduralSfx.Tick(), 0.4f, 1.2f);
                    break;
                case MiniEventKind.Perfect:
                    break;
                case MiniEventKind.Good:
                    stage.Play(ForgeProceduralSfx.LogSplit(), 0.6f, 1f);
                    stage.Burst(12);
                    view.Popup("통나무 " + game.LogsCut + "개 완료!", new Color(1f, 0.9f, 0.4f), position);
                    break;
                case MiniEventKind.Miss:
                    if (miniEvent.Extra > 0f)
                    {
                        stage.Play(ForgeProceduralSfx.Creak(), 0.5f, 1f);
                        stage.Shake(0.015f, 0.15f);
                    }
                    view.Popup(miniEvent.Extra > 0f ? "톱이 끼었다!" : "너무 약하다", new Color(1f, 0.5f, 0.4f), position);
                    if (miniEvent.Extra > 0f)
                    {
                        view.Flash(0.06f, new Color(1f, 0.3f, 0.25f));
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

        protected override string ResultText(BowSawGame game)
        {
            return $"활 깎기 {Mathf.RoundToInt(game.Accuracy * 100f)}점\n" +
                $"<size=60%>켠 통나무 {game.LogsCut}개 / {game.LogsTarget}개   톱질 {game.Strokes}번   톱 낌 {game.Binds}회</size>";
        }
    }
}
