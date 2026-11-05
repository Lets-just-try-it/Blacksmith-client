using System;
using Blacksmith.UI;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Blacksmith.Battle
{
    // 호스트가 파견을 확정하면 전투 씬을 낮 씬 위에 Additive로 불러오고, 결과를 반영하면 내린다.
    // 낮 씬 오브젝트이며 호스트가 소유한다. 참가자는 Fusion 씬 관리자가 호스트를 따라 불러오고 내린다. (docs/NETWORK_DESIGN.md 8절)
    // 불러오고 내리기 전에 모든 참가자의 화면을 어둡게 하고, 호스트 화면이 다 어두워지면 씬을 바꾼다. 밝히는 쪽은 DaySceneBattleCover다.
    public class BattleSceneLoader : NetworkBehaviour
    {
        [SerializeField] private BattleChannel _channel;
        [SerializeField] private string _battleScenePath = "Assets/01.Scenes/Battle.unity";
        [SerializeField] private ScreenFadeChannel _screenFade;

        // 호스트가 씬을 바꾸려고 화면을 어둡게 할 때마다 늘린다. 참가자는 이 값이 바뀌면 화면을 어둡게 한다.
        [Networked] private int FadeOutCount { get; set; }

        private ChangeDetector _changes;

        // 전투 씬을 불러온 뒤 내리기 전까지. 호스트에서만 쓴다.
        private bool _isLoaded;

        // 호스트에서만 불린다. 스폰된 뒤 파견할 수 있을 때다.
        public event Action HostReady;

        public bool CanDispatch => Object != null && Object.IsValid && HasStateAuthority && !_isLoaded;

        private void Awake()
        {
            _channel.Clear();
        }

        private void OnEnable()
        {
            _channel.ResultConfirmed += Unload;
        }

        private void OnDisable()
        {
            _channel.ResultConfirmed -= Unload;
        }

        public override void Spawned()
        {
            _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
            if (HasStateAuthority)
            {
                HostReady?.Invoke();
            }
        }

        // 호스트 전용. 파견 정보는 전투 씬이 스폰될 때 꺼내 쓴다.
        public bool Dispatch(DispatchRequest request)
        {
            int sceneIndex = SceneUtility.GetBuildIndexByScenePath(_battleScenePath);
            if (!CanDispatch || sceneIndex < 0)
            {
                if (sceneIndex < 0)
                {
                    Debug.LogError($"빌드 설정에 전투 씬이 없다: {_battleScenePath}", this);
                }

                return false;
            }

            _isLoaded = true;
            _channel.SetPendingDispatch(request);
            ChangeSceneAfterFadeOut(runner => runner.LoadScene(SceneRef.FromIndex(sceneIndex), LoadSceneMode.Additive));
            return true;
        }

        public override void Render()
        {
            foreach (string change in _changes.DetectChanges(this))
            {
                // 호스트는 씬을 바꾸는 쪽에서 직접 어둡게 하고 끝나기를 기다린다.
                if (change == nameof(FadeOutCount) && !HasStateAuthority)
                {
                    _ = _screenFade.FadeOut();
                }
            }
        }

        // 호스트 전용. 화면이 어두워지는 사이 세션이 끝났으면 씬을 바꾸지 않는다.
        private async void ChangeSceneAfterFadeOut(Action<NetworkRunner> changeScene)
        {
            FadeOutCount++;
            await _screenFade.FadeOut();
            if (Runner != null && Runner.IsRunning)
            {
                changeScene(Runner);
            }
        }

        // 호스트에서만 불린다. 결과를 반영한 직후다.
        private void Unload(BattleOutcome outcome)
        {
            if (!HasStateAuthority || !_isLoaded)
            {
                return;
            }

            _isLoaded = false;
            SceneRef battleScene = SceneRef.FromIndex(SceneUtility.GetBuildIndexByScenePath(_battleScenePath));
            ChangeSceneAfterFadeOut(runner => runner.UnloadScene(battleScene));
        }
    }
}
