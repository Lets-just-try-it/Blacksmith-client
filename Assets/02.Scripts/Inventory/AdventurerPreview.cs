using Blacksmith.Battle;
using Blacksmith.Core.Adventurers;
using Blacksmith.Core.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace Blacksmith.Inventory
{
    // 장비 착용 창에 고른 모험가의 모습을 보여 준다. 화면 밖에 둔 무대에서 직업의 전투 모델을 전용 카메라로 찍어 RawImage에 그린다.
    // 입은 장비는 아이템 색을 칠한 상자로 머리·몸·손·목 위치에 붙인다. 장비 모델이 생기면 바꾼다.
    public class AdventurerPreview : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("직업별 전투 모델을 찾는 데 쓴다")]
        [SerializeField] private AdventurerBattleData _battle;
        [SerializeField] private ItemCatalog _catalog;
        [SerializeField] private Camera _camera;
        [Tooltip("모델과 장비 상자의 부모. 천천히 돌린다")]
        [SerializeField] private Transform _modelRoot;
        [SerializeField] private RawImage _target;
        [SerializeField] private Renderer _helmet;
        [SerializeField] private Renderer _armor;
        [SerializeField] private Renderer _weapon;
        [SerializeField] private Renderer _accessory;
        [Tooltip("초당 도는 각도")]
        [SerializeField] private float _turnSpeed = 30f;

        private RenderTexture _texture;
        private MaterialPropertyBlock _properties;
        private BattleUnitView _model;
        private int _modelJobId;

        private void Awake()
        {
            _properties = new MaterialPropertyBlock();
            _camera.enabled = false;
        }

        private void OnDestroy()
        {
            if (_texture != null)
            {
                _texture.Release();
                Destroy(_texture);
            }
        }

        // 표시 전용이라 Time을 쓴다.
        private void Update()
        {
            _modelRoot.Rotate(0f, _turnSpeed * Time.deltaTime, 0f, Space.Self);
        }

        public void Show(AdventurerData adventurer)
        {
            gameObject.SetActive(true);
            EnsureTexture();
            _camera.enabled = true;
            SetModel(adventurer != null ? adventurer.JobId : 0);
            SetEquipment(_helmet, adventurer, EquipmentSlot.Helmet);
            SetEquipment(_armor, adventurer, EquipmentSlot.Armor);
            SetEquipment(_weapon, adventurer, EquipmentSlot.Weapon);
            SetEquipment(_accessory, adventurer, EquipmentSlot.Accessory);
        }

        public void Hide()
        {
            _camera.enabled = false;
            gameObject.SetActive(false);
        }

        private void EnsureTexture()
        {
            if (_texture != null)
            {
                return;
            }

            Rect rect = _target.rectTransform.rect;
            _texture = new RenderTexture(Mathf.Max(64, (int)rect.width * 2), Mathf.Max(64, (int)rect.height * 2), 16);
            _camera.targetTexture = _texture;
            _target.texture = _texture;
        }

        // 직업이 바뀔 때만 모델을 새로 만든다.
        private void SetModel(int jobId)
        {
            if (_model != null && _modelJobId == jobId)
            {
                return;
            }

            if (_model != null)
            {
                Destroy(_model.gameObject);
                _model = null;
            }

            _modelJobId = jobId;
            BattleUnitView prefab = jobId != 0 ? _battle.GetViewPrefab(jobId) : null;
            if (prefab != null)
            {
                _model = Instantiate(prefab, _modelRoot);
                _model.transform.localPosition = Vector3.zero;
                _model.transform.localRotation = Quaternion.identity;
            }
        }

        private void SetEquipment(Renderer target, AdventurerData adventurer, EquipmentSlot slot)
        {
            ItemStack equipped = adventurer != null ? EquipmentRules.GetEquipped(adventurer, slot) : ItemStack.Empty;
            ItemData item = equipped.IsEmpty ? null : _catalog.Find(equipped.ItemId);
            target.gameObject.SetActive(item != null);
            if (item != null)
            {
                target.GetPropertyBlock(_properties);
                _properties.SetColor(BaseColorId, item.Color);
                target.SetPropertyBlock(_properties);
            }
        }
    }
}
