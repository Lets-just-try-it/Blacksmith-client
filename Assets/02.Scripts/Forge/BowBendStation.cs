using System.Text;
using Blacksmith.Core.Forge;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 활 몸체 휘기 작업대(활 제작의 첫 단계). 달군 쇠막대를 활 모양 형틀에 맞춰 마디마다 눌러 휜다.
    public class BowBendStation : MiniStation<BowBendGame>
    {
        [SerializeField] private BowBendRulesData _rulesData;

        private readonly StringBuilder _builder = new StringBuilder();
        private BowBendRules _rules;
        private int _listKey = int.MinValue;

        protected override void OnAwake()
        {
            _rules = _rulesData.ToRules();
        }

        protected override BowBendGame CreateGame(int seed, int difficulty)
        {
            _listKey = int.MinValue;
            return new BowBendGame(_rules, seed, difficulty);
        }

        protected override string Objective =>
            "달군 쇠막대를 형틀에 맞춰 휘자!\n• 마디(가로 여섯 칸)를 누른 채 마우스를 아래로 끌어 내리면 그만큼 휜다. 한 번 휜 쇠는 되돌릴 수 없다\n• 노란 눈금선(형틀)에 맞추되, 놓으면 조금 되돌아온다. 쇠가 식을수록 더 되돌아온다\n• 마디마다 열이 있어(눈금 아래 막대) 시간이 지나면 식는다. 식으면 같은 거리를 끌어도 덜 휜다\n• E를 누르고 있으면 가리키는 마디를 불에 달군다 (달구는 동안은 휘지 못한다)\n• 초록 띠 안에 놓으면 굳는다. 지나치게 휘면 그대로 굳고, 덜 휘면 다시 끌어 더 휠 수 있다";

        protected override void Refresh(BowBendGame game, MiniPanelView view)
        {
            ((BowBendStage)Stage).Show(game, Time.deltaTime);
            view.SetChip(0, "굳은 마디  " + game.LockedCount + " / " + BowBendGame.SegmentCount);
            view.SetChip(1, "지나치게 휨  " + game.Overbent + "회", game.Overbent > 0 ? new Color(0.3f, 0.1f, 0.08f, 0.95f) : (Color?)null);
            view.SetChip(2, "쇠 열기  " + Mathf.RoundToInt(game.AverageHeat * 100f) + "%");
            RefreshStatus(game, view);
            RefreshList(game, view);
            RefreshScore(game, view);
            RefreshTime(game, view);
        }

        private static void RefreshStatus(BowBendGame game, MiniPanelView view)
        {
            string message;
            Color color = new Color(1f, 0.85f, 0.5f);
            switch (game.Phase)
            {
                case BowBendGame.PhaseKind.Intro:
                    message = "활 몸체 휘기 시작! 곧 달군 쇠막대가 나온다";
                    break;
                case BowBendGame.PhaseKind.Bend:
                    if (game.Heating >= 0)
                    {
                        message = "달구는 중... 열 " + Mathf.RoundToInt(game.HeatOf(game.Heating) * 100f) + "%";
                        color = new Color(1f, 0.7f, 0.4f);
                    }
                    else if (game.Active >= 0)
                    {
                        float bend = game.BendOf(game.Active);
                        float target = game.TargetOf(game.Active);
                        if (game.HeatOf(game.Active) < 0.35f)
                        {
                            message = "쇠가 식었다! 뻑뻑하고 더 되돌아온다 (놓고 E로 달구자)";
                            color = new Color(0.7f, 0.8f, 1f);
                        }
                        else if (bend * (1f - game.SpringBack) > target + game.Tolerance)
                        {
                            message = "너무 휘었다! 지금 놓으면 지나치게 굳는다";
                            color = new Color(1f, 0.5f, 0.4f);
                        }
                        else if (bend < target * 0.8f)
                        {
                            message = "더 끌어 내리자...";
                        }
                        else
                        {
                            message = "거의 다 왔다. 놓으면 조금 되돌아온다";
                            color = new Color(0.8f, 1f, 0.7f);
                        }
                    }
                    else if (game.Hovered >= 0 && game.IsLocked(game.Hovered))
                    {
                        message = "이 마디는 이미 굳었다";
                    }
                    else
                    {
                        message = game.Hovered >= 0 && game.HeatOf(game.Hovered) < 0.4f ? "이 마디가 식었다. E로 달구자" : "마디를 눌러 아래로 끌어 휘자 (E: 달구기)";
                    }
                    break;
                default:
                    message = game.TimedOut ? "쇠가 식었다!" : "끝!";
                    color = new Color(1f, 0.95f, 0.8f);
                    break;
            }

            view.SetStatus(message, color);
        }

        private void RefreshList(BowBendGame game, MiniPanelView view)
        {
            int key = game.LockedCount * 1000 + game.Overbent * 100 + game.Reheats * 10 + (game.Active + 1);
            if (key == _listKey)
            {
                return;
            }

            _listKey = key;
            _builder.Clear();
            _builder.Append("<size=130%><color=#FFD973>활 몸체 휘기 기록</color></size>\n");
            _builder.Append("<color=#99FFB3>굳은 마디  ").Append(game.LockedCount).Append(" / ").Append(BowBendGame.SegmentCount).Append("</color>\n");
            _builder.Append(game.Overbent > 0 ? "<color=#FF8877>" : "<color=#AAAAAA>").Append("지나치게 휨  ").Append(game.Overbent).Append("</color>\n");
            view.SetList(_builder.ToString());
        }

        private static void RefreshScore(BowBendGame game, MiniPanelView view)
        {
            float value = game.Progress;
            if (game.Phase == BowBendGame.PhaseKind.Intro || (game.LockedCount == 0 && game.Active < 0))
            {
                view.SetScore("지금까지 점수  -", new Color(1f, 1f, 1f, 0.7f));
                return;
            }

            Color color = value >= game.SuccessScore ? new Color(0.5f, 0.92f, 0.45f) : (value >= 0.3f ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.55f, 0.45f));
            view.SetScore("지금까지 점수  " + Mathf.RoundToInt(value * 100f) + "점", color);
        }

        private static void RefreshTime(BowBendGame game, MiniPanelView view)
        {
            if (game.Phase != BowBendGame.PhaseKind.Bend)
            {
                view.SetTime(false, 0f, string.Empty, Color.white);
                return;
            }

            view.SetTime(true, game.TimeLeft / game.TimeLimit, "남은 시간 " + game.TimeLeft.ToString("0.0"), game.TimeLeft < 8f ? new Color(1f, 0.5f, 0.4f) : new Color(1f, 1f, 1f, 0.7f));
        }

        protected override void HandleEvent(BowBendGame game, MiniEvent miniEvent, MiniStage stage, MiniPanelView view, MiniBoardView board)
        {
            Vector2 position = board.ToLocal(0f, -1.2f);
            switch (miniEvent.Kind)
            {
                case MiniEventKind.PhaseChanged:
                    stage.Play(ForgeProceduralSfx.Tick(), 0.4f, 1.2f);
                    break;
                case MiniEventKind.Charge:
                    stage.Play(ForgeProceduralSfx.Creak(), 0.25f, 1.4f);
                    break;
                case MiniEventKind.Swing:
                    stage.Play(ForgeProceduralSfx.Whoosh(), 0.3f, 0.6f);
                    break;
                case MiniEventKind.Hit:
                    stage.Play(ForgeProceduralSfx.Clang((int)(miniEvent.Index % 3)), 0.45f, 1f);
                    stage.Burst(10);
                    view.Popup("딱 맞았다!", new Color(0.6f, 1f, 0.7f), position);
                    break;
                case MiniEventKind.Miss:
                    stage.Play(ForgeProceduralSfx.Fail(), 0.35f, 1.3f);
                    stage.Shake(0.012f, 0.12f);
                    view.Popup("너무 휘었다!", new Color(1f, 0.5f, 0.4f), position);
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

        protected override string ResultText(BowBendGame game)
        {
            return $"활 몸체 휘기 {Mathf.RoundToInt(game.Accuracy * 100f)}점\n" +
                $"<size=60%>굳은 마디 {game.LockedCount}개 / {BowBendGame.SegmentCount}개   지나치게 휨 {game.Overbent}회</size>";
        }
    }
}
