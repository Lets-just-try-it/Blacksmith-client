using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Blacksmith.Dialogue
{
    // 모든 참가자가 함께 보는 대화(컷씬, 튜토리얼 등)를 연다. 씬 오브젝트이며 호스트가 소유한다.
    // 대사는 각자 자기 속도로 넘기고, 시작할 때 접속해 있던 참가자가 모두 다 읽으면 함께 닫는다.
    public class SharedDialogue : NetworkBehaviour, IPlayerLeft
    {
        [SerializeField] private DialogueCatalog _catalog;
        [SerializeField] private DialogueChannel _channel;

        // 호스트에서만 쓴다.
        private readonly HashSet<PlayerRef> _waitingFor = new HashSet<PlayerRef>();
        private DialogueData _playing;
        private int _playId;

        private DialogueRequest _localRequest;
        private int _localPlayId;

        // 호스트에서만 불린다. 튜토리얼·컷씬이 다음 단계로 넘어갈 때 쓴다.
        public event Action<DialogueData> Finished;

        public bool IsPlaying => _playing != null;

        public override void Spawned()
        {
            _channel.Register(this);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (_localRequest != null)
            {
                _channel.Close(_localRequest);
            }

            _channel.Unregister(this);
        }

        // 호스트 전용. 이미 함께 보는 대화가 진행 중이면 열지 않는다.
        public bool Play(DialogueData dialogue)
        {
            if (!HasStateAuthority || IsPlaying)
            {
                return false;
            }

            int index = _catalog.IndexOf(dialogue);
            if (index < 0)
            {
                Debug.LogWarning($"{dialogue.name}이 DialogueCatalog에 없어 함께 볼 수 없다.", this);
                return false;
            }

            _playing = dialogue;
            _playId++;
            _waitingFor.Clear();
            foreach (PlayerRef player in Runner.ActivePlayers)
            {
                _waitingFor.Add(player);
            }

            RpcPlay(index, _playId);
            return true;
        }

        // 호스트 전용. DialoguePlayerActions가 참가자의 종료 알림을 받아 부른다.
        public void MarkFinished(PlayerRef player, int playId)
        {
            if (HasStateAuthority && IsPlaying && playId == _playId && _waitingFor.Remove(player))
            {
                CloseIfAllFinished();
            }
        }

        public void PlayerLeft(PlayerRef player)
        {
            if (HasStateAuthority && IsPlaying && _waitingFor.Remove(player))
            {
                CloseIfAllFinished();
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RpcPlay(int index, int playId)
        {
            if (!_catalog.TryGet(index, out DialogueData dialogue))
            {
                Debug.LogWarning($"DialogueCatalog에 {index}번 대화가 없다. 참가자끼리 빌드가 다를 수 있다.", this);
                return;
            }

            var request = new DialogueRequest(dialogue, isShared: true);
            request.LinesFinished = () => SendFinished(playId);
            request.Closed = reason =>
            {
                if (_localRequest == request)
                {
                    _localRequest = null;
                }
            };

            _localRequest = request;
            _localPlayId = playId;
            if (!_channel.Play(request))
            {
                // 열지 못한 참가자 때문에 모두가 기다리지 않도록 바로 다 읽은 것으로 알린다.
                _localRequest = null;
                SendFinished(playId);
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RpcAllFinished(int playId)
        {
            if (_localRequest != null && _localPlayId == playId)
            {
                _channel.Close(_localRequest);
            }
        }

        private void SendFinished(int playId)
        {
            if (_channel.LocalPlayer != null)
            {
                _channel.LocalPlayer.SendSharedFinished(playId);
            }
            else
            {
                Debug.LogWarning("내 플레이어가 아직 없어 함께 보는 대화 종료를 알리지 못했다.", this);
            }
        }

        private void CloseIfAllFinished()
        {
            if (_waitingFor.Count > 0)
            {
                return;
            }

            DialogueData finished = _playing;
            _playing = null;
            RpcAllFinished(_playId);
            Finished?.Invoke(finished);
        }
    }
}
