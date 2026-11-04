using System;
using System.Collections.Generic;
using Blacksmith.Core;
using Blacksmith.Core.Sales;
using Blacksmith.Network;
using Fusion;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 가게의 손님을 호스트가 맡는다. 낮에는 진열 여부와 상관없이 손님을 보내고, 낮이 끝나면 손님을 내보낸다.
    // 낮·밤 단계는 GameState가 정하고, 마감 동안 진행 중인 미니게임을 기다리는 일은 ForgeDayCycleLink가 맡는다.
    public class ShopFlow : NetworkBehaviour
    {
        [SerializeField] private SalesRulesData _rulesData;
        [SerializeField] private GameState _gameState;
        [SerializeField] private DisplayShelfState[] _shelves;
        [SerializeField] private NetworkObject _customerPrefab;
        [Tooltip("손님이 들어오고 나가는 자리")]
        [SerializeField] private Transform _entrance;
        [Tooltip("진열대 말고 손님이 구경하러 들르는 빈 자리들")]
        [SerializeField] private Transform[] _wanderPoints;

        private readonly List<Customer> _customers = new List<Customer>();
        private SalesRules _rules;
        private System.Random _random;
        private TickTimer _spawnTimer;

        public int CustomerCount => _customers.Count;

        public override void Spawned()
        {
            if (!HasStateAuthority)
            {
                return;
            }

            _rules = _rulesData.ToRules();
            _random = new System.Random(Environment.TickCount);
            _spawnTimer = TickTimer.CreateFromSeconds(Runner, _rules.CustomerInterval);
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || !_gameState.IsReady)
            {
                return;
            }

            if (_gameState.Phase == DayPhase.Day)
            {
                TrySpawnCustomer();
            }
            else
            {
                DismissCustomers();
            }
        }

        // 진열대가 비어 있어도 손님은 온다. 들를 곳은 진열대 앞과 빈 자리 중에서 무작위로 정한다.
        private void TrySpawnCustomer()
        {
            if (!_spawnTimer.ExpiredOrNotRunning(Runner))
            {
                return;
            }

            _spawnTimer = TickTimer.CreateFromSeconds(Runner, _rules.CustomerInterval);
            if (_customers.Count >= _rules.MaxCustomers)
            {
                return;
            }

            int[] plan = CustomerChoice.PlanRoute(_shelves.Length + _wanderPoints.Length, _rules.MinStops, _rules.MaxStops, _random);
            var route = new Customer.Stop[plan.Length];
            for (int i = 0; i < plan.Length; i++)
            {
                int index = plan[i];
                route[i] = index < _shelves.Length
                    ? new Customer.Stop(_shelves[index].CustomerPoint, _shelves[index])
                    : new Customer.Stop(_wanderPoints[index - _shelves.Length].position, null);
            }

            Vector3 entrance = _entrance.position;
            Runner.Spawn(_customerPrefab, entrance + Vector3.up, Quaternion.identity, null, (runner, spawned) =>
            {
                var customer = spawned.GetComponent<Customer>();
                customer.Init(route, entrance, _rules, _random, _gameState, OnCustomerGone);
                _customers.Add(customer);
            });
        }

        private void DismissCustomers()
        {
            foreach (Customer customer in _customers)
            {
                customer.Dismiss();
            }
        }

        private void OnCustomerGone(Customer customer)
        {
            _customers.Remove(customer);
        }

    }
}
