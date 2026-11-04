using Blacksmith.Core;
using Blacksmith.Network;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Blacksmith.Forge
{
    // 낮·밤 단계에 맞춰 낮 씬의 해(Directional Light)와 환경광을 바꾼다. 밤으로 넘어가는 연출이 시작되면 어두워지고,
    // 다음 날 낮이 시작되면 다시 밝아진다. 모든 참가자의 낮 씬에 둔다. 표시 전용이라 Update에서 서서히 바꾼다.
    public class DayNightLighting : MonoBehaviour
    {
        [SerializeField] private GameState _gameState;
        [SerializeField] private Light _sun;
        [SerializeField, Min(0f)] private float _nightIntensityScale = 0.25f;
        [SerializeField] private Color _nightColor = new Color(0.55f, 0.62f, 1f, 1f);
        [Tooltip("밤에 환경광(색 또는 스카이박스 세기)에 곱하는 값")]
        [SerializeField, Range(0f, 1f)] private float _nightAmbientScale = 0.35f;
        [Tooltip("낮↔밤 조명이 바뀌는 시간(초)")]
        [SerializeField, Min(0.01f)] private float _transitionSeconds = 1.5f;

        private float _dayIntensity;
        private Color _dayColor;
        private Color _dayAmbient;
        private float _dayAmbientIntensity;
        // 0이면 낮 조명, 1이면 밤 조명이다.
        private float _night;

        private void Awake()
        {
            _dayIntensity = _sun.intensity;
            _dayColor = _sun.color;
            _dayAmbient = RenderSettings.ambientLight;
            _dayAmbientIntensity = RenderSettings.ambientIntensity;
        }

        private void OnDisable()
        {
            _night = 0f;
            Apply();
        }

        private void Update()
        {
            float target = _gameState.IsReady && DayCycleRules.IsNightLook(_gameState.Phase) ? 1f : 0f;
            if (Mathf.Approximately(_night, target))
            {
                return;
            }

            _night = Mathf.MoveTowards(_night, target, Time.deltaTime / _transitionSeconds);
            Apply();
        }

        private void Apply()
        {
            // 씬을 내릴 때는 해가 먼저 파괴되었을 수 있다.
            if (_sun == null)
            {
                return;
            }

            _sun.intensity = _dayIntensity * Mathf.Lerp(1f, _nightIntensityScale, _night);
            _sun.color = Color.Lerp(_dayColor, _nightColor, _night);
            // 전투 씬이 활성 씬인 동안에는 환경광이 전투 씬 것이므로 건드리지 않는다.
            if (SceneManager.GetActiveScene() == gameObject.scene)
            {
                float ambientScale = Mathf.Lerp(1f, _nightAmbientScale, _night);
                RenderSettings.ambientLight = _dayAmbient * ambientScale;
                RenderSettings.ambientIntensity = _dayAmbientIntensity * ambientScale;
            }
        }
    }
}
