using Blacksmith.Core.Forge;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 담금질 칼날 연출. 달군 칼날을 세워 들고 있다가 수조로 옮겨 눕혀 담그고, 꺼내면 식은 색으로 바뀐다.
    // 색은 꺼내기 전까지 그대로 둔다. 식는 모습이 기준 시간의 힌트가 되면 안 된다.
    public class QuenchBladeVisual : MonoBehaviour
    {
        [SerializeField] private Transform _blade;
        [SerializeField] private Renderer _bladeRenderer;
        [SerializeField] private Transform _heldPose;
        [SerializeField] private Transform _waterPose;
        [SerializeField] private Color _hotColor = new Color(1f, 0.45f, 0.1f);
        [SerializeField] private Color _coolColor = new Color(0.35f, 0.37f, 0.4f);
        [SerializeField, Min(0.01f)] private float _liftTime = 0.4f;

        private MaterialPropertyBlock _block;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
        }

        public void SetVisible(bool visible)
        {
            _blade.gameObject.SetActive(visible);
        }

        public void Apply(QuenchGame game, float carryTime)
        {
            switch (game.Phase)
            {
                case QuenchGame.PhaseKind.Ready:
                    SetPose(_heldPose, _heldPose, 0f);
                    SetColor(_hotColor);
                    break;
                case QuenchGame.PhaseKind.Dunk:
                    SetPose(_heldPose, _waterPose, game.PhaseClock / Mathf.Max(0.01f, carryTime));
                    SetColor(_hotColor);
                    break;
                case QuenchGame.PhaseKind.Done:
                    SetPose(_waterPose, _heldPose, game.PhaseClock / _liftTime);
                    SetColor(_coolColor);
                    break;
            }
        }

        private void SetPose(Transform from, Transform to, float t)
        {
            float smooth = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            _blade.SetPositionAndRotation(
                Vector3.Lerp(from.position, to.position, smooth),
                Quaternion.Slerp(from.rotation, to.rotation, smooth));
        }

        private void SetColor(Color color)
        {
            _bladeRenderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColor, color);
            _bladeRenderer.SetPropertyBlock(_block);
        }
    }
}
