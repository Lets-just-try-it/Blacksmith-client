using TMPro;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 몬스터가 쓰러진 자리에서 튀어 올라 바닥에 떨어지는 전리품 하나다. 표시 전용이다.
    public class BattleLootDropView : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("아이템 색으로 칠하는 상자. 아이콘 모델이 생기기 전까지 쓴다")]
        [SerializeField] private Renderer _renderer;
        [Tooltip("날아가는 동안 돌리고 떨어진 뒤 떠 있게 할 대상")]
        [SerializeField] private Transform _visual;
        [SerializeField] private TMP_Text _label;
        [SerializeField, Min(0f)] private float _popHeight = 1.2f;
        [SerializeField, Min(0.05f)] private float _flyTime = 0.45f;
        [SerializeField] private float _spinSpeed = 120f;
        [SerializeField, Min(0f)] private float _bobHeight = 0.08f;
        [SerializeField, Min(0f)] private float _bobSpeed = 3f;

        private Vector3 _from;
        private Vector3 _to;
        private float _elapsed;

        public void Play(Vector3 from, Vector3 to, Color color, string label, Quaternion cameraRotation)
        {
            _from = from;
            _to = to;
            _elapsed = 0f;
            transform.position = from;

            var block = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            _renderer.SetPropertyBlock(block);

            _label.text = label;
            _label.color = Color.Lerp(color, Color.white, 0.7f);
            // 쿼터뷰 카메라는 회전하지 않아 한 번만 맞춘다.
            _label.transform.rotation = cameraRotation;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _flyTime);
            Vector3 position = Vector3.Lerp(_from, _to, t);
            position.y += _popHeight * 4f * t * (1f - t);
            if (t >= 1f)
            {
                position.y += _bobHeight * (1f + Mathf.Sin((_elapsed - _flyTime) * _bobSpeed)) * 0.5f;
            }

            transform.position = position;
            _visual.Rotate(0f, _spinSpeed * Time.deltaTime, 0f, Space.World);
        }
    }
}
