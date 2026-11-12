using System.Text;
using Blacksmith.Core.Forge;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 시험 사격 작업대(활 제작의 마지막 단계). 제한시간 동안 시위를 당겨 볏짚 허수아비를 최대한 많이 맞힌다.
    public class BowRangeStation : MiniStation<BowRangeGame>
    {
        [SerializeField] private BowRangeRulesData _rulesData;

        private readonly StringBuilder _builder = new StringBuilder();
        private BowRangeRules _rules;
        private int _listKey = int.MinValue;
        private int _landed;

        protected override void OnAwake()
        {
            _rules = _rulesData.ToRules();
        }

        protected override BowRangeGame CreateGame(int seed, int difficulty)
        {
            _listKey = int.MinValue;
            _landed = 0;
            return new BowRangeGame(_rules, seed, difficulty);
        }

        protected override string Objective =>
            "제한시간 안에 볏짚 허수아비를 최대한 많이 맞히자! (가슴 1점, 머리 2점)\n• 마우스로 가슴을 겨누고, 왼쪽 버튼을 누르고 있으면 시위가 당겨진다. 다 당긴 뒤 놓으면 발사\n• 시위를 당기는 동안 E(스페이스바)를 누르면 숨을 참아 손 떨림이 줄어든다. 숨이 다하면 헐떡여 더 흔들린다\n• 먼 허수아비는 화살이 처진다. 조준점 아래 가로 눈금(점 하나가 5m)에 가슴 가운데를 맞추자\n• 바람은 화살을 옆으로 민다. 옆의 깃발과 위쪽 바람 표시, 좌우 세로 눈금을 보고 바람 쪽으로 겨냥하자\n• 머리를 맞히면 치명타! 2점이다. 맞히면 다음 허수아비가 나타난다. 45초 동안 계속 쏘고, 목표 점수를 채워도 끝나지 않는다(점수는 계속 쌓인다)";

        protected override void Refresh(BowRangeGame game, MiniPanelView view)
        {
            ((BowRangeStage)Stage).Show(game, Board, Time.deltaTime);
            ShowLanding(game, view);
            view.SetChip(0, "점수  " + game.Points + " / " + game.GoalHits);
            view.SetChip(1, "거리 " + Mathf.RoundToInt(game.Distance) + "m  바람 " + WindText(game.Wind));
            view.SetChip(2, "당김  " + Mathf.RoundToInt(game.Power * 100f) + "%");
            RefreshStatus(game, view);
            RefreshList(game, view);
            RefreshScore(game, view);
            RefreshTime(game, view);
        }

        // 화살이 날아가는 시간이 지나 허수아비에 꽂히는 순간에 소리와 결과를 보여 준다.
        private void ShowLanding(BowRangeGame game, MiniPanelView view)
        {
            if (game.Phase != BowRangeGame.PhaseKind.Result || game.PhaseClock < game.FlightTime || _landed >= game.Shots)
            {
                return;
            }

            _landed = game.Shots;
            MiniStage stage = Stage;
            Vector2 position = Board.ToLocal(game.TargetX, game.TargetY - game.TargetRadius - 1.4f);
            stage.Play(ForgeProceduralSfx.Thud(), 0.6f, game.LastHit ? 1f : 0.6f);
            stage.Shake(0.01f, 0.1f);
            if (game.LastHead)
            {
                // 치명타: 묵직한 쇠 소리와 높은 종소리, 화면이 확 다가왔다 돌아오며 크게 흔들리고 번쩍인다.
                stage.Play(ForgeProceduralSfx.Clang(2), 0.7f, 0.8f);
                stage.Play(ForgeProceduralSfx.Ding(2100f), 0.5f, 1f);
                stage.Play(ForgeProceduralSfx.Boom(), 0.5f, 1.3f);
                stage.Shake(0.03f, 0.25f);
                stage.Punch(0.09f);
                stage.Burst(45);
                view.Popup("치명타!  +2", new Color(1f, 0.35f, 0.25f), position);
                view.Flash(0.14f, new Color(1f, 0.8f, 0.5f));
            }
            else if (game.LastHit)
            {
                stage.Play(ForgeProceduralSfx.Ding(1400f), 0.3f, 1f);
                stage.Burst(14);
                view.Popup(game.Streak >= 2 ? "명중!  " + game.Streak + "연속" : "명중!", new Color(1f, 0.9f, 0.4f), position);
                view.Flash(0.05f, new Color(1f, 0.95f, 0.7f));
            }
            else
            {
                view.Popup("빗나갔다", new Color(1f, 0.5f, 0.4f), position);
            }
        }

        private static string WindText(float wind)
        {
            if (Mathf.Abs(wind) < 0.15f)
            {
                return "잔잔함";
            }

            return (wind < 0f ? "← " : "→ ") + Mathf.Abs(wind).ToString("0.0");
        }

        private static void RefreshStatus(BowRangeGame game, MiniPanelView view)
        {
            string message;
            Color color = new Color(1f, 0.85f, 0.5f);
            switch (game.Phase)
            {
                case BowRangeGame.PhaseKind.Intro:
                    message = "시험 사격 시작! 곧 허수아비가 나온다";
                    break;
                case BowRangeGame.PhaseKind.Aim:
                    message = "허수아비 가슴을 겨누고 눌러서 시위를 당기자";
                    break;
                case BowRangeGame.PhaseKind.Draw:
                    if (game.Power < 1f)
                    {
                        message = "시위를 당기는 중... 다 당긴 뒤 놓자";
                    }
                    else
                    {
                        bool tired = game.DrawClock > 1.4f;
                        message = game.Gasping ? "숨이 가쁘다! 흔들린다" : (game.HoldingBreath ? "숨을 참는 중... 지금이다!" : (tired ? "팔이 떨린다! 빨리 놓자 (E: 숨 참기)" : "지금이다! 놓자 (E: 숨 참기)"));
                        color = (tired || game.Gasping) ? new Color(1f, 0.5f, 0.4f) : new Color(0.6f, 1f, 0.7f);
                    }
                    break;
                case BowRangeGame.PhaseKind.Result:
                    if (game.PhaseClock < game.FlightTime)
                    {
                        message = "화살이 날아간다...";
                    }
                    else
                    {
                        message = game.LastHead ? "치명타! 머리 명중 +2점" : (game.LastHit ? "명중! +1점" : "빗나갔다");
                        color = game.LastHead ? new Color(1f, 0.45f, 0.3f) : (game.LastHit ? new Color(0.6f, 1f, 0.7f) : new Color(1f, 0.5f, 0.4f));
                    }
                    break;
                default:
                    message = "시간이 다 됐다!";
                    color = new Color(1f, 0.95f, 0.8f);
                    break;
            }

            view.SetStatus(message, color);
        }

        private void RefreshList(BowRangeGame game, MiniPanelView view)
        {
            int key = game.Shots * 1000 + game.Points * 100 + game.Headshots * 10 + game.Streak;
            if (key == _listKey)
            {
                return;
            }

            _listKey = key;
            _builder.Clear();
            _builder.Append("<size=130%><color=#FFD973>시험 사격 기록</color></size>\n");
            _builder.Append("<color=#FF9977>치명타(머리)  ").Append(game.Headshots).Append("</color>\n");
            _builder.Append("<color=#99FFB3>점수  ").Append(game.Points).Append(" / ").Append(game.GoalHits).Append("</color>\n");
            _builder.Append("<color=#AAAAAA>쏜 화살  ").Append(game.Shots).Append("발</color>\n");
            _builder.Append("<color=#FFD973>연속 명중  ").Append(game.Streak).Append("</color>\n");
            view.SetList(_builder.ToString());
        }

        private static void RefreshScore(BowRangeGame game, MiniPanelView view)
        {
            if (game.Shots == 0)
            {
                view.SetScore("지금까지 점수  -", new Color(1f, 1f, 1f, 0.7f));
                return;
            }

            float value = game.Progress;
            Color color = value >= game.SuccessScore ? new Color(0.5f, 0.92f, 0.45f) : (value >= 0.4f ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.55f, 0.45f));
            view.SetScore("지금까지 점수  " + Mathf.RoundToInt(value * 100f) + "점", color);
        }

        private static void RefreshTime(BowRangeGame game, MiniPanelView view)
        {
            bool running = game.Phase != BowRangeGame.PhaseKind.Intro && game.Phase != BowRangeGame.PhaseKind.Done;
            if (!running)
            {
                view.SetTime(false, 0f, string.Empty, Color.white);
                return;
            }

            view.SetTime(true, game.TimeLeft / game.TimeLimit, "남은 시간 " + game.TimeLeft.ToString("0.0"), game.TimeLeft < 10f ? new Color(1f, 0.5f, 0.4f) : new Color(1f, 1f, 1f, 0.7f));
        }

        protected override void HandleEvent(BowRangeGame game, MiniEvent miniEvent, MiniStage stage, MiniPanelView view, MiniBoardView board)
        {
            switch (miniEvent.Kind)
            {
                case MiniEventKind.PhaseChanged:
                    stage.Play(ForgeProceduralSfx.Tick(), 0.3f, 1.2f);
                    break;
                case MiniEventKind.Charge:
                    stage.Play(ForgeProceduralSfx.Creak(), 0.18f, 1.7f);
                    break;
                case MiniEventKind.Perfect:
                case MiniEventKind.Hit:
                case MiniEventKind.Miss:
                    stage.Play(ForgeProceduralSfx.Twang(), 0.55f, 1f);
                    stage.Play(ForgeProceduralSfx.Whoosh(), 0.35f, 1.5f);
                    stage.Shake(0.006f, 0.08f);
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

        protected override string ResultText(BowRangeGame game)
        {
            return $"시험 사격 {Mathf.RoundToInt(game.Accuracy * 100f)}점\n" +
                $"<size=60%>점수 {game.Points}/{game.GoalHits}   명중 {game.Hits}발(치명타 {game.Headshots})   쏜 화살 {game.Shots}발</size>";
        }
    }
}
