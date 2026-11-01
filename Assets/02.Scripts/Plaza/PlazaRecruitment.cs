using System;
using System.Collections.Generic;
using Blacksmith.Core.Adventurers;
using Fusion;
using UnityEngine;

namespace Blacksmith.Plaza
{
    // 광장의 영입 후보를 뽑아 세우고, 영입 요청을 처리한다. 씬 오브젝트이며 호스트에서만 동작한다.
    public class PlazaRecruitment : NetworkBehaviour
    {
        [SerializeField] private AdventurerGenerationData _generation;
        [SerializeField] private AdventurerRosterState _roster;
        [SerializeField] private RecruitCandidate _candidatePrefab;
        [Tooltip("후보가 서는 자리. 최대 후보 수보다 적으면 남는 후보는 세우지 않는다.")]
        [SerializeField] private Transform[] _spawnPoints = new Transform[0];
        [Tooltip("0이면 세션마다 다른 후보가 나온다. 같은 후보로 확인하고 싶을 때만 정한다.")]
        [SerializeField] private int _fixedSeed;

        private readonly List<RecruitCandidate> _candidates = new List<RecruitCandidate>();

        public override void Spawned()
        {
            if (HasStateAuthority)
            {
                SpawnCandidates(_fixedSeed != 0 ? _fixedSeed : Environment.TickCount);
            }
        }

        // 호스트 전용. 남아 있는 후보를 치우고 새로 뽑는다. 낮·밤 사이클이 생기면 낮이 시작될 때 부른다.
        public void SpawnCandidates(int seed)
        {
            if (!HasStateAuthority)
            {
                return;
            }

            ClearCandidates();
            List<AdventurerData> candidates = AdventurerGenerator.GenerateCandidates(seed, _generation.ToRules());
            if (candidates.Count > _spawnPoints.Length)
            {
                Debug.LogWarning($"후보 {candidates.Count}명을 세울 자리가 {_spawnPoints.Length}곳뿐이다.", this);
            }

            int count = Mathf.Min(candidates.Count, _spawnPoints.Length);
            for (int i = 0; i < count; i++)
            {
                AdventurerData data = candidates[i];
                Transform spawnPoint = _spawnPoints[i];
                RecruitCandidate candidate = Runner.Spawn(_candidatePrefab, spawnPoint.position, spawnPoint.rotation,
                    onBeforeSpawned: (runner, candidateObject) => candidateObject.GetComponent<RecruitCandidate>().Initialize(this, data));
                _candidates.Add(candidate);
            }
        }

        // 호스트 전용. 같은 후보에 요청이 겹치면 먼저 처리한 요청만 성공한다.
        public bool TryRecruit(RecruitCandidate candidate)
        {
            if (!HasStateAuthority || !_candidates.Contains(candidate))
            {
                return false;
            }

            // [미정] 대장간 명성 등에 따른 성공·실패. 지금은 빈 슬롯이 있으면 항상 성공한다.
            if (!_roster.TryAdd(candidate.Data.ToData()))
            {
                Debug.Log("관리 슬롯이 가득 차 영입하지 못했다.", this);
                return false;
            }

            _candidates.Remove(candidate);
            Runner.Despawn(candidate.Object);
            return true;
        }

        private void ClearCandidates()
        {
            foreach (RecruitCandidate candidate in _candidates)
            {
                if (candidate != null)
                {
                    Runner.Despawn(candidate.Object);
                }
            }

            _candidates.Clear();
        }
    }
}
