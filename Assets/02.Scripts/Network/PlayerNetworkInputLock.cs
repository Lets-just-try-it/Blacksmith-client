using System.Collections.Generic;
using UnityEngine;

namespace Blacksmith.Network
{
    // 잠긴 동안 PlayerInputCollector가 틱마다 호스트로 보내는 PlayerNetworkInput(이동, 버튼)을 빈 값으로 보낸다.
    // 키 입력과 RPC는 막지 않는다. 미니게임·대화창처럼 로컬에서 입력 액션을 직접 읽는 기능은 잠긴 동안에도 그대로 동작한다.
    // 캐릭터 조작을 멈춰야 하는 기능은 모두 이 에셋을 쓴다. 사용법은 docs/NETWORK_DESIGN.md "캐릭터 조작 입력 잠금"을 따른다.
    [CreateAssetMenu(fileName = "PlayerNetworkInputLock", menuName = "Blacksmith/Runtime/Player Network Input Lock")]
    public class PlayerNetworkInputLock : ScriptableObject
    {
        // 잠근 쪽마다 따로 기록해, 한쪽이 풀어도 다른 쪽의 잠금은 남게 한다.
        private readonly HashSet<object> _owners = new HashSet<object>();

        public bool IsLocked => _owners.Count > 0;

        public void Lock(object owner)
        {
            _owners.Add(owner);
        }

        // 잠그지 않은 쪽이 불러도 안전하다.
        public void Release(object owner)
        {
            _owners.Remove(owner);
        }

        // 에디터에서 도메인 리로드 없이 플레이를 다시 시작하면 이전 세션의 잠금이 남으므로 세션 시작 때 비운다.
        public void Clear()
        {
            _owners.Clear();
        }
    }
}
