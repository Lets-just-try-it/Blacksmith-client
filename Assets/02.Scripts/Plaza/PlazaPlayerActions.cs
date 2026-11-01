using Blacksmith.Network;
using Fusion;
using UnityEngine;

namespace Blacksmith.Plaza
{
    // Player 프리팹에 붙는 광장 행동 컴포넌트다. 가까운 영입 후보를 찾고, 상호작용 키로 호스트에 영입을 요청한다.
    public class PlazaPlayerActions : NetworkBehaviour
    {
        [SerializeField, Min(0f)] private float _interactRadius = 1.5f;

        private readonly Collider[] _hits = new Collider[16];
        private RecruitCandidate _promptedCandidate;

        [Networked] private NetworkButtons PreviousButtons { get; set; }

        public override void FixedUpdateNetwork()
        {
            if (!GetInput(out PlayerNetworkInput input))
            {
                return;
            }

            bool interactPressed = input.Buttons.WasPressed(PreviousButtons, PlayerButton.Interact);
            PreviousButtons = input.Buttons;

            // 예측 재계산 중에 같은 요청을 다시 보내지 않도록 처음 계산하는 틱에서만 보낸다.
            if (!interactPressed || !HasInputAuthority || !Runner.IsForward)
            {
                return;
            }

            RecruitCandidate candidate = FindNearestCandidate();
            if (candidate != null)
            {
                RpcRequestRecruit(candidate.Object.Id);
            }
        }

        // 안내 표시는 내 캐릭터 기준으로 내 화면에만 띄운다.
        public override void Render()
        {
            if (HasInputAuthority)
            {
                ShowPromptOn(FindNearestCandidate());
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            ShowPromptOn(null);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RpcRequestRecruit(NetworkId candidateId)
        {
            if (Runner.TryFindObject(candidateId, out NetworkObject candidateObject)
                && candidateObject.TryGetComponent(out RecruitCandidate candidate))
            {
                candidate.RequestRecruit();
            }
        }

        private RecruitCandidate FindNearestCandidate()
        {
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, _interactRadius, _hits,
                Physics.AllLayers, QueryTriggerInteraction.Collide);

            RecruitCandidate nearest = null;
            float nearestSqrDistance = float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                if (!_hits[i].TryGetComponent(out RecruitCandidate candidate))
                {
                    continue;
                }

                float sqrDistance = (candidate.transform.position - transform.position).sqrMagnitude;
                if (sqrDistance < nearestSqrDistance)
                {
                    nearest = candidate;
                    nearestSqrDistance = sqrDistance;
                }
            }

            return nearest;
        }

        private void ShowPromptOn(RecruitCandidate candidate)
        {
            if (candidate == _promptedCandidate)
            {
                return;
            }

            if (_promptedCandidate != null)
            {
                _promptedCandidate.SetPromptVisible(false);
            }

            _promptedCandidate = candidate;
            if (_promptedCandidate != null)
            {
                _promptedCandidate.SetPromptVisible(true);
            }
        }
    }
}
