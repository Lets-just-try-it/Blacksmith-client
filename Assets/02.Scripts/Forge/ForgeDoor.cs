using System.Collections.Generic;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 캐릭터가 트리거 안에 들어오면 문짝이 캐릭터 반대쪽으로 서서히 열리고, 모두 나가면 서서히 닫힌다.
    // 표시 전용이라 네트워크 상태를 두지 않고, 각 클라이언트가 자기 화면에서 열고 닫는다.
    [RequireComponent(typeof(BoxCollider))]
    public class ForgeDoor : MonoBehaviour
    {
        [SerializeField] private Transform[] _hinges;
        [SerializeField] private float _openAngle = 100f;
        [SerializeField] private float _angularSpeed = 200f;

        private Vector3[] _leafDirections;
        private float[] _signs;
        private float _current;
        private float _target;
        private BoxCollider _trigger;
        private readonly List<Collider> _occupants = new List<Collider>();

        private void Awake()
        {
            _leafDirections = new Vector3[_hinges.Length];
            _signs = new float[_hinges.Length];
            for (int i = 0; i < _hinges.Length; i++)
            {
                Vector3 toLeaf = _hinges[i].GetChild(0).position - _hinges[i].position;
                _leafDirections[i] = transform.InverseTransformDirection(toLeaf.normalized);
            }
            _trigger = GetComponent<BoxCollider>();
            enabled = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<CharacterController>() == null) return;

            if (_occupants.Count == 0 && Mathf.Approximately(_current, 0f)) PickSwingSide(other.transform.position);
            if (!_occupants.Contains(other)) _occupants.Add(other);
            _target = _openAngle;
            enabled = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<CharacterController>() == null) return;

            _occupants.Remove(other);
            if (_occupants.Count > 0) return;
            _target = 0f;
            enabled = true;
        }

        private void Update()
        {
            // 캐릭터가 파괴되거나 순간이동하면 OnTriggerExit가 오지 않을 수 있어, 트리거 안에 아직 있는지 직접 확인한다.
            _occupants.RemoveAll(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy || !_trigger.bounds.Intersects(c.bounds));
            if (_occupants.Count == 0) _target = 0f;

            _current = Mathf.MoveTowards(_current, _target, _angularSpeed * Time.deltaTime);
            for (int i = 0; i < _hinges.Length; i++)
                _hinges[i].localRotation = Quaternion.Euler(0f, _signs[i] * _current, 0f);

            if (_occupants.Count == 0 && Mathf.Approximately(_current, 0f))
                enabled = false;
        }

        // 문 앞면(forward) 기준으로 캐릭터가 있는 쪽의 반대로 문짝 끝이 움직이게 방향을 정한다.
        private void PickSwingSide(Vector3 playerPosition)
        {
            float side = Mathf.Sign(Vector3.Dot(transform.forward, playerPosition - transform.position));
            Vector3 away = -side * Vector3.forward;
            for (int i = 0; i < _hinges.Length; i++)
            {
                // 위(Y)축 양의 회전은 문짝 방향 e를 cross(up, e) 쪽으로 움직인다.
                Vector3 tipMotion = Vector3.Cross(Vector3.up, _leafDirections[i]);
                _signs[i] = Mathf.Sign(Vector3.Dot(tipMotion, away));
            }
        }
    }
}
