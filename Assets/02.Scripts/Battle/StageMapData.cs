using Blacksmith.Core.Battle;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 스테이지 맵 생성 수치와 바닥 머티리얼 (docs/GAME_CONCEPT.md 7.2 "맵 생성"). 던전 데이터가 참조한다.
    [CreateAssetMenu(fileName = "StageMap", menuName = "Blacksmith/Battle/Stage Map")]
    public class StageMapData : ScriptableObject
    {
        [Header("덩어리 (칸 단위)")]
        [Tooltip("[추정] 맵의 긴 변 길이 범위")]
        [SerializeField, Min(10)] private int _minSize = 180;
        [SerializeField, Min(10)] private int _maxSize = 240;
        [Tooltip("[추정] 이어 붙이는 타원 덩어리 수 범위")]
        [SerializeField, Min(1)] private int _minLobes = 4;
        [SerializeField, Min(1)] private int _maxLobes = 7;
        [Tooltip("[추정] 덩어리 하나의 반지름 범위")]
        [SerializeField, Min(2)] private int _minLobeRadius = 32;
        [SerializeField, Min(2)] private int _maxLobeRadius = 56;
        [Tooltip("[추정] 이웃 덩어리 중심 거리 = 두 반지름 합 × 이 비율(%). 작을수록 많이 겹쳐 이음목이 넓다")]
        [SerializeField, Range(10, 100)] private int _lobeSpacingPercent = 60;

        [Header("변형 (칸 단위)")]
        [Tooltip("[추정] 맵 전체를 크게 휘는 정도")]
        [SerializeField, Min(0f)] private float _warpAmplitude = 16f;
        [Tooltip("[추정] 크게 휘는 굽이 하나의 크기. 클수록 완만하다")]
        [SerializeField, Min(1f)] private float _warpScale = 90f;
        [Tooltip("[추정] 가장자리를 울퉁불퉁하게 하는 정도")]
        [SerializeField, Min(0f)] private float _edgeAmplitude = 6f;
        [Tooltip("[추정] 가장자리 굴곡 하나의 크기")]
        [SerializeField, Min(1f)] private float _edgeScale = 18f;
        [Tooltip("[추정] 이보다 좁은 목과 돌기는 지운다. 좁은 길처럼 느껴지지 않게 넓게 둔다")]
        [SerializeField, Min(1)] private int _minWidth = 24;
        [Tooltip("변형 뒤 남은 바닥이 덩어리 넓이의 이 비율(%)보다 작으면 다시 만든다")]
        [SerializeField, Range(0, 100)] private int _minKeptPercent = 70;

        [Header("바닥")]
        [Tooltip("[추정] 바닥 한 칸의 월드 크기")]
        [SerializeField, Min(0.1f)] private float _cellSize = 1f;
        [Tooltip("이 던전의 바닥 머티리얼. 맵 통일성을 위해 던전마다 하나만 쓴다")]
        [SerializeField] private Material _floorMaterial;
        [Tooltip("바닥 텍스처 한 장이 덮는 월드 크기")]
        [SerializeField, Min(0.1f)] private float _textureWorldSize = 4f;

        public float CellSize => _cellSize;
        public Material FloorMaterial => _floorMaterial;
        public float TextureWorldSize => _textureWorldSize;

        public StageMapSettings ToSettings()
        {
            return new StageMapSettings
            {
                MinSize = _minSize,
                MaxSize = Mathf.Max(_minSize, _maxSize),
                MinLobes = _minLobes,
                MaxLobes = Mathf.Max(_minLobes, _maxLobes),
                MinLobeRadius = _minLobeRadius,
                MaxLobeRadius = Mathf.Max(_minLobeRadius, _maxLobeRadius),
                LobeSpacingPercent = _lobeSpacingPercent,
                WarpAmplitude = _warpAmplitude,
                WarpScale = _warpScale,
                EdgeAmplitude = _edgeAmplitude,
                EdgeScale = _edgeScale,
                MinWidth = _minWidth,
                MinKeptPercent = _minKeptPercent,
            };
        }
    }
}
