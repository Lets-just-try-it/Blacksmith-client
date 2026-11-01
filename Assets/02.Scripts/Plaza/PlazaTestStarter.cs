using System;
using Blacksmith.Network;
using Fusion;
using UnityEngine;

namespace Blacksmith.Plaza
{
    // PlazaTest 씬을 혼자 확인하기 위한 진입점이다. 로비를 거치지 않고 참가자가 없는 호스트로 시작한다.
    // NetworkBootstrap의 Game 씬 경로를 PlazaTest로 지정해 둔다.
    public class PlazaTestStarter : MonoBehaviour
    {
        [SerializeField] private NetworkBootstrap _bootstrap;

        private async void Start()
        {
            // 호스트가 시작되면 이 씬을 네트워크 씬으로 다시 불러온다. 그때는 이미 러너가 있으므로 다시 시작하지 않는다.
            if (NetworkRunner.Instances.Count > 0)
            {
                return;
            }

            // 같은 App ID를 쓰는 팀원과 세션이 겹치지 않게 이름을 매번 새로 만든다.
            await _bootstrap.StartHost($"PlazaTest-{Guid.NewGuid():N}");
        }
    }
}
