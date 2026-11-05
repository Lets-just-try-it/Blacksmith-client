using System;
using Blacksmith.Characters;
using Blacksmith.Core.Characters;
using Blacksmith.Core.Sales;
using Blacksmith.Network;
using Fusion;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 판매 구역을 돌아다니는 손님이다. 입구에서 들어와 정해진 길(진열대 앞과 빈 자리)을 따라 곳곳에서 잠깐씩 둘러본다.
    // 장비가 올라간 진열대 앞에서는 일정 확률로만 사고, 사면 나간다. 안 사면 다음 곳으로 간다.
    // 움직임과 구매는 호스트만 처리하고, 위치는 NetworkTransform이 복제한다.
    public class Customer : NetworkBehaviour
    {
        // 손님이 들르는 곳. 진열대 앞이면 Shelf가 있고, 그냥 구경하는 자리면 비어 있다.
        public readonly struct Stop
        {
            public readonly Vector3 Point;
            public readonly DisplayShelfState Shelf;

            public Stop(Vector3 point, DisplayShelfState shelf)
            {
                Point = point;
                Shelf = shelf;
            }
        }

        public enum Stage
        {
            Entering,
            Browsing,
            Leaving,
        }

        [SerializeField] private HumanoidAppearanceView _view;

        [Networked] public Stage CurrentStage { get; private set; }
        [Networked] public NetworkHumanoidAppearance Appearance { get; private set; }
        [Networked] private TickTimer BrowseTimer { get; set; }

        private Stop[] _route;
        private int _stopIndex;
        private Vector3 _exitPoint;
        private SalesRules _rules;
        private System.Random _random;
        private GameState _gameState;
        private Action<Customer> _onGone;

        // 스폰되기 전(onBeforeSpawned)에 호스트가 부른다.
        public void Init(Stop[] route, Vector3 exitPoint, HumanoidAppearance appearance, SalesRules rules, System.Random random,
            GameState gameState, Action<Customer> onGone)
        {
            Appearance = NetworkHumanoidAppearance.FromAppearance(appearance);
            _route = route;
            _exitPoint = exitPoint;
            _rules = rules;
            _random = random;
            _gameState = gameState;
            _onGone = onGone;
        }

        public override void Spawned()
        {
            _view.Apply(Appearance.ToAppearance());
            if (HasStateAuthority)
            {
                CurrentStage = _route.Length > 0 ? Stage.Entering : Stage.Leaving;
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _onGone?.Invoke(this);
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
            {
                return;
            }

            switch (CurrentStage)
            {
                case Stage.Entering:
                    if (WalkTo(_route[_stopIndex].Point))
                    {
                        BrowseTimer = TickTimer.CreateFromSeconds(Runner, _rules.BrowseTime);
                        CurrentStage = Stage.Browsing;
                    }
                    break;
                case Stage.Browsing:
                    if (BrowseTimer.Expired(Runner))
                    {
                        FinishBrowsing();
                    }
                    break;
                case Stage.Leaving:
                    if (WalkTo(_exitPoint))
                    {
                        Runner.Despawn(Object);
                    }
                    break;
            }
        }

        // 가게를 닫는다: 하던 일을 멈추고 나간다.
        public void Dismiss()
        {
            if (HasStateAuthority && CurrentStage != Stage.Leaving)
            {
                CurrentStage = Stage.Leaving;
            }
        }

        private void FinishBrowsing()
        {
            if (TryBuyHere())
            {
                CurrentStage = Stage.Leaving;
                return;
            }

            _stopIndex++;
            CurrentStage = _stopIndex < _route.Length ? Stage.Entering : Stage.Leaving;
        }

        // 진열대 앞이고 올려 둔 물건이 있으면 확률로 사서 골드를 더한다.
        private bool TryBuyHere()
        {
            DisplayShelfState shelf = _route[_stopIndex].Shelf;
            if (shelf == null || shelf.IsEmpty || !CustomerChoice.WillBuy(_rules.BuyChance, _random))
            {
                return false;
            }

            if (!shelf.TryBuy(out int price))
            {
                return false;
            }

            _gameState.TryAddGold(price);
            return true;
        }

        // 도착하면 true. 높이는 그대로 두고 바닥 위에서만 걷는다.
        private bool WalkTo(Vector3 point)
        {
            Vector3 position = transform.position;
            var target = new Vector3(point.x, position.y, point.z);
            Vector3 next = Vector3.MoveTowards(position, target, _rules.WalkSpeed * Runner.DeltaTime);
            Vector3 direction = next - position;
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            transform.position = next;
            return (next - target).sqrMagnitude < 0.0001f;
        }
    }
}
