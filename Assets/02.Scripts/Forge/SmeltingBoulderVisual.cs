using System;
using System.Collections.Generic;
using Blacksmith.Core.Forge;
using UnityEngine;

namespace Blacksmith.Forge
{
    // ① 쪼개기 단계의 원석, 광맥, 곡괭이와 쪼개진 뒤 도가니로 날아가는 쇳덩이를 맡는다.
    // 규칙은 건드리지 않고 SmeltingGame의 값을 읽어 모양만 바꾼다. 좌표는 모두 SmeltingStage 로컬이다.
    public class SmeltingBoulderVisual
    {
        private const float PickIdle = 40f;
        private const float PickLength = 1.5f;
        // 곡괭이 머리 가운데에서 왼쪽 뾰족한 끝까지의 위치
        private static readonly Vector3 PickTip = new Vector3(-0.684f, 0.121f, 0f);
        // 곡괭이가 내려가기 시작해서 원석에 닿을 때까지의 시간
        private const float ImpactDelay = 0.065f;
        private const float SplitTime = 0.6f;
        private const int NuggetCount = 8;

        private sealed class Flyer
        {
            public Transform Transform;
            public Vector3 From;
            public Vector3 To;
            public float Progress;
            public float Duration;
            public float Arc;
        }

        private struct PendingImpact
        {
            public float At;
            public int Index;
            public float Quality;
        }

        private readonly SmeltingGreybox _kit;
        private readonly Transform _stage;
        private readonly Transform _boulder;
        private readonly GameObject _block;
        private readonly Transform _pick;
        private readonly List<Renderer> _pickHead = new List<Renderer>();
        private readonly List<Transform> _rockParts = new List<Transform>();
        private readonly List<Renderer> _rockRenderers = new List<Renderer>();
        private readonly List<Vector3> _rockSizes = new List<Vector3>();
        private readonly List<Vector3> _rockOffsets = new List<Vector3>();
        private readonly List<Renderer> _veins = new List<Renderer>();
        private readonly List<Transform> _cracks = new List<Transform>();
        private readonly List<Flyer> _flyers = new List<Flyer>();
        private readonly List<PendingImpact> _impacts = new List<PendingImpact>();
        private SmeltingGame _game;
        private Vector2 _pointer;
        private Vector2 _pointerSmooth;
        private float _swing = -1f;
        private float _pickAngle = PickIdle;
        private float _jolt;
        private float _split = -1f;

        // 곡괭이가 원석에 닿았다(로컬 위치, 품질, 콤보를 반영한 세기)
        public event Action<Vector3, float, float> Impacted;
        // 쪼개진 쇳덩이가 도가니에 떨어졌다(로컬 위치)
        public event Action<Vector3> NuggetLanded;

        public SmeltingBoulderVisual(SmeltingGreybox kit, Transform stage)
        {
            _kit = kit;
            _stage = stage;

            var boulderObject = new GameObject("Boulder");
            boulderObject.transform.SetParent(stage, false);
            boulderObject.transform.localPosition = SmeltingLayout.BoulderPos;
            _boulder = boulderObject.transform;

            _block = kit.Box(stage, "BoulderBlock", SmeltingLayout.BoulderPos + new Vector3(0f, -1.15f, 0f), new Vector3(2.7f, 0.5f, 2.3f), new Color(0.25f, 0.25f, 0.28f));
            BuildRocks();
            BuildVeins();

            var pickObject = new GameObject("Pick");
            pickObject.transform.SetParent(stage, false);
            _pick = pickObject.transform;
            kit.Box(_pick, "PickHandle", new Vector3(0f, PickLength * 0.5f, 0f), new Vector3(0.14f, PickLength, 0.14f), new Color(0.5f, 0.32f, 0.16f));
            BuildPickHead(kit);
        }

        // 곡괭이 머리: 자루 끝의 굵은 마디에서 양쪽으로 자루 쪽으로 휘며 뾰족해진다. 왼쪽 끝(PickTip)이 내리찍는 곳이다.
        private void BuildPickHead(SmeltingGreybox kit)
        {
            const int segments = 10;
            const float radius = 2f;
            const float halfAngle = 20f;
            Color iron = new Color(0.3f, 0.3f, 0.34f);
            for (int i = 0; i < segments; i++)
            {
                float a0 = Mathf.Lerp(-halfAngle, halfAngle, i / (float)segments) * Mathf.Deg2Rad;
                float a1 = Mathf.Lerp(-halfAngle, halfAngle, (i + 1) / (float)segments) * Mathf.Deg2Rad;
                Vector3 p0 = new Vector3(radius * Mathf.Sin(a0), radius * (1f - Mathf.Cos(a0)), 0f);
                Vector3 p1 = new Vector3(radius * Mathf.Sin(a1), radius * (1f - Mathf.Cos(a1)), 0f);
                Vector3 delta = p1 - p0;
                float middle = 1f - Mathf.Abs((i + 0.5f) / segments * 2f - 1f);
                float thickness = Mathf.Lerp(0.05f, 0.24f, Mathf.Pow(middle, 0.7f));
                GameObject piece = kit.Box(_pick, "PickHead" + i, (p0 + p1) * 0.5f, new Vector3(delta.magnitude * 1.12f, thickness, 0.22f), iron);
                piece.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                _pickHead.Add(piece.GetComponent<Renderer>());
            }

            // 자루를 끼운 굵은 마디
            GameObject collar = kit.Box(_pick, "PickCollar", new Vector3(0f, 0.04f, 0f), new Vector3(0.3f, 0.34f, 0.3f), new Color(0.22f, 0.22f, 0.25f));
            _pickHead.Add(collar.GetComponent<Renderer>());
        }

        // 광석 종류에 따라 돌 색이 다르다: 철(갈색), 은철(푸른 은빛), 용철(붉은 빛)
        private static Color[] Tones(int ore)
        {
            if (ore == 1)
            {
                return new[] { new Color(0.55f, 0.6f, 0.7f), new Color(0.5f, 0.55f, 0.66f), new Color(0.6f, 0.65f, 0.74f) };
            }
            if (ore >= 2)
            {
                return new[] { new Color(0.42f, 0.2f, 0.22f), new Color(0.35f, 0.17f, 0.2f), new Color(0.48f, 0.24f, 0.26f) };
            }
            return new[] { new Color(0.40f, 0.35f, 0.32f), new Color(0.35f, 0.31f, 0.29f), new Color(0.45f, 0.39f, 0.34f) };
        }

        private void BuildRocks()
        {
            Color[] tones = Tones(0);
            Vector3[] offsets =
            {
                Vector3.zero, new Vector3(0.65f, 0.35f, 0.1f), new Vector3(-0.7f, 0.2f, -0.1f), new Vector3(0.1f, 0.75f, 0f),
                new Vector3(0.5f, -0.45f, -0.2f), new Vector3(-0.5f, -0.5f, 0.2f), new Vector3(-0.15f, 0.2f, -0.55f), new Vector3(0.2f, -0.1f, 0.5f),
            };
            Vector3[] sizes =
            {
                new Vector3(1.9f, 1.6f, 1.6f), new Vector3(1f, 1.1f, 1f), new Vector3(1.1f, 1f, 1.1f), new Vector3(1f, 0.9f, 1f),
                new Vector3(1f, 0.8f, 1f), new Vector3(1f, 0.8f, 1f), new Vector3(1f, 1f, 0.8f), new Vector3(1f, 0.9f, 0.8f),
            };

            var random = new System.Random(7);
            for (int i = 0; i < offsets.Length; i++)
            {
                GameObject rock = _kit.Box(_boulder, "Rock" + i, offsets[i], sizes[i], tones[i % tones.Length]);
                rock.transform.localRotation = Quaternion.Euler(Range(random, -14f, 14f), Range(random, 0f, 90f), Range(random, -14f, 14f));
                _rockParts.Add(rock.transform);
                _rockRenderers.Add(rock.GetComponent<Renderer>());
                _rockSizes.Add(sizes[i]);
                _rockOffsets.Add(offsets[i]);
            }
        }

        // 빛나는 광맥은 카메라를 향한 좌표 평면 위에 둔다.
        private void BuildVeins()
        {
            Vector2[] positions =
            {
                new Vector2(-0.5f, 0.3f), new Vector2(0.4f, 0.55f), new Vector2(0.1f, -0.35f), new Vector2(-0.75f, -0.4f), new Vector2(0.75f, -0.15f),
            };
            var random = new System.Random(8);
            Quaternion facing = Quaternion.LookRotation(-SmeltingLayout.ArenaNormal, SmeltingLayout.ArenaUp);
            foreach (Vector2 position in positions)
            {
                Vector3 point = SmeltingLayout.BoulderPos + SmeltingLayout.ArenaNormal * SmeltingLayout.SurfaceDepth
                    + SmeltingLayout.ArenaRight * (position.x * 1.1f) + SmeltingLayout.ArenaUp * (position.y * 1.1f);
                GameObject vein = _kit.Box(_stage, "Vein", point, new Vector3(0.75f, 0.07f, 0.07f), new Color(1f, 0.6f, 0.2f));
                vein.transform.localRotation = facing * Quaternion.Euler(0f, 0f, Range(random, -60f, 60f));
                vein.transform.SetParent(_boulder, true);
                _veins.Add(vein.GetComponent<Renderer>());
            }
        }

        public void Bind(SmeltingGame game)
        {
            _game = game;
        }

        public void SetPointer(float x, float y)
        {
            _pointer = new Vector2(x, y);
        }

        public void SetActive(bool active)
        {
            _boulder.gameObject.SetActive(active);
            _block.SetActive(active);
            _pick.gameObject.SetActive(active);
        }

        // 새로 시작할 때 쪼개진 원석과 금을 원래대로 되돌린다.
        public void ResetVisual(int ore)
        {
            Color[] tones = Tones(ore);
            for (int i = 0; i < _rockRenderers.Count; i++)
            {
                _kit.Paint(_rockRenderers[i], tones[i % tones.Length], Color.black);
            }

            foreach (Transform crack in _cracks)
            {
                UnityEngine.Object.Destroy(crack.gameObject);
            }
            _cracks.Clear();
            foreach (Flyer flyer in _flyers)
            {
                UnityEngine.Object.Destroy(flyer.Transform.gameObject);
            }
            _flyers.Clear();
            _impacts.Clear();

            for (int i = 0; i < _rockParts.Count; i++)
            {
                _rockParts[i].localPosition = _rockOffsets[i];
                _rockParts[i].localScale = _rockSizes[i];
            }
            _split = -1f;
            _swing = -1f;
            _jolt = 0f;
            _pickAngle = PickIdle;
            _boulder.localScale = Vector3.one;
            _boulder.localPosition = SmeltingLayout.BoulderPos;
            SetActive(true);
        }

        public void OnStrike(int index, float quality)
        {
            _swing = 0f;
            _impacts.Add(new PendingImpact { At = Time.time + ImpactDelay, Index = index, Quality = quality });
        }

        public void Swing()
        {
            _swing = 0f;
        }

        // 마지막 균열까지 쳤다: 원석이 쪼개지고 쇳덩이가 도가니로 날아간다.
        public void Split()
        {
            _split = 0f;
            var random = new System.Random(3);
            for (int i = 0; i < NuggetCount; i++)
            {
                GameObject nugget = _kit.Box(_stage, "FlyNugget", Vector3.zero, new Vector3(0.36f, 0.3f, 0.34f), new Color(0.9f, 0.55f, 0.25f));
                _kit.Paint(nugget.GetComponent<Renderer>(), new Color(0.9f, 0.55f, 0.25f), new Color(1f, 0.5f, 0.1f) * 1.3f);
                Vector3 from = SmeltingLayout.BoulderPos + new Vector3(Range(random, -0.5f, 0.5f), Range(random, -0.3f, 0.6f), Range(random, -0.3f, 0.3f));
                Vector3 to = SmeltingLayout.PitPos + new Vector3(Range(random, -0.3f, 0.3f), 0.55f, Range(random, -0.3f, 0.3f));
                nugget.transform.localPosition = from;
                _flyers.Add(new Flyer
                {
                    Transform = nugget.transform,
                    From = from,
                    To = to,
                    Progress = -i * 0.07f,
                    Duration = 0.9f + Range(random, 0f, 0.15f),
                    Arc = 2.2f + Range(random, 0f, 0.6f),
                });
            }
        }

        public void Tick(float deltaTime)
        {
            if (_game == null)
            {
                return;
            }

            UpdateImpacts();
            UpdateBoulder(deltaTime);
            UpdatePick(deltaTime);
            UpdateFlyers(deltaTime);
        }

        public Vector3 BoulderCenter => _boulder.parent.TransformPoint(SmeltingLayout.BoulderPos);

        private void UpdateImpacts()
        {
            for (int i = _impacts.Count - 1; i >= 0; i--)
            {
                if (Time.time < _impacts[i].At)
                {
                    continue;
                }

                DoImpact(_impacts[i]);
                _impacts.RemoveAt(i);
            }
        }

        private void DoImpact(PendingImpact impact)
        {
            SmeltingGame.Target target = _game.Targets[impact.Index];
            Vector3 point = SmeltingLayout.ArenaToLocal(target.X, target.Y);
            float strength = impact.Quality * (1f + Mathf.Clamp01(_game.Combo / 10f) * 0.5f);
            _jolt = Mathf.Max(_jolt, Mathf.Clamp01(0.4f + 0.4f * strength));
            AddCrack(SmeltingLayout.SurfaceToLocal(target.X, target.Y));
            Impacted?.Invoke(point, impact.Quality, strength);
        }

        private void AddCrack(Vector3 point)
        {
            GameObject crack = _kit.Box(_boulder, "Crack", Vector3.zero, new Vector3(0.55f, 0.05f, 0.04f), new Color(0.08f, 0.06f, 0.06f));
            crack.transform.position = _stage.TransformPoint(point + SmeltingLayout.ArenaNormal * 0.01f);
            crack.transform.rotation = _stage.rotation * Quaternion.LookRotation(-SmeltingLayout.ArenaNormal, SmeltingLayout.ArenaUp) * Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 180f));
            _kit.Paint(crack.GetComponent<Renderer>(), new Color(0.06f, 0.05f, 0.05f), new Color(1f, 0.45f, 0.1f) * 0.9f);
            _cracks.Add(crack.transform);
        }

        private void UpdateBoulder(float deltaTime)
        {
            _jolt = Mathf.MoveTowards(_jolt, 0f, deltaTime * 5f);

            // 맞을 때마다 들썩이고, 쪼개질 때는 조각들이 날아간다.
            _boulder.localScale = Vector3.one * (1f - 0.05f * _jolt);
            _boulder.localPosition = SmeltingLayout.BoulderPos + new Vector3(0f, -0.04f * _jolt, 0f);

            // 광맥은 아직 안 맞춘 균열이 많을수록 밝다.
            float left = 1f - (float)_game.Judged / _game.Targets.Count;
            float twinkle = 0.75f + 0.25f * Mathf.Sin(Time.time * 4f);
            foreach (Renderer vein in _veins)
            {
                _kit.Paint(vein, new Color(1f, 0.6f, 0.2f), new Color(1f, 0.55f, 0.15f) * (0.7f + 1.6f * left * twinkle));
            }

            if (_split < 0f)
            {
                return;
            }

            _split += deltaTime;
            float k = Mathf.Clamp01(_split / SplitTime);
            for (int i = 0; i < _rockParts.Count; i++)
            {
                Vector3 direction = _rockOffsets[i].sqrMagnitude < 0.001f ? Vector3.up : _rockOffsets[i].normalized;
                _rockParts[i].localPosition = _rockOffsets[i] + direction * (1.8f * k) + Vector3.down * (2.2f * k * k);
                _rockParts[i].localScale = _rockSizes[i] * (1f - k);
            }
            if (k >= 1f)
            {
                _split = -1f;
                _boulder.gameObject.SetActive(false);
                _block.SetActive(false);
            }
        }

        private void UpdatePick(float deltaTime)
        {
            bool show = _game.Act == SmeltingGame.ActKind.Crack && !_game.Finished;
            if (_pick.gameObject.activeSelf != show)
            {
                _pick.gameObject.SetActive(show);
            }
            if (!show)
            {
                return;
            }

            _pointerSmooth = Vector2.Lerp(_pointerSmooth, _pointer, 1f - Mathf.Exp(-34f * deltaTime));

            // 스윙: 내리찍은 뒤 천천히 들어 올린다.
            if (_swing >= 0f)
            {
                float from = PickIdle;
                _swing += deltaTime;
                if (_swing < 0.07f)
                {
                    _pickAngle = Mathf.Lerp(from, -8f, _swing / 0.07f);
                }
                else if (_swing < 0.12f)
                {
                    _pickAngle = -8f;
                }
                else if (_swing < 0.42f)
                {
                    float k = (_swing - 0.12f) / 0.3f;
                    _pickAngle = Mathf.Lerp(-8f, PickIdle, 1f - (1f - k) * (1f - k));
                }
                else
                {
                    _swing = -1f;
                    _pickAngle = PickIdle;
                }
            }
            else
            {
                _pickAngle = Mathf.Lerp(_pickAngle, PickIdle, 1f - Mathf.Exp(-10f * deltaTime));
            }

            Vector3 right = SmeltingLayout.ArenaRight;
            Vector3 up = SmeltingLayout.ArenaUp;
            Vector3 point = SmeltingLayout.ArenaToLocal(_pointerSmooth.x, _pointerSmooth.y) + SmeltingLayout.ArenaNormal * 0.3f;
            Vector3 hand = point + up * PickLength;
            float angle = _pickAngle * Mathf.Deg2Rad;
            Vector3 head = hand - (right * Mathf.Sin(angle) + up * Mathf.Cos(angle)) * PickLength;
            _pick.localRotation = Quaternion.LookRotation(-SmeltingLayout.ArenaNormal, up) * Quaternion.Euler(0f, 0f, -_pickAngle);
            // 머리의 왼쪽 뾰족한 끝이 가리키는 곳에 오도록 맞춘다.
            _pick.localPosition = head - _pick.localRotation * PickTip;

            float hot = Mathf.Clamp01(_game.Combo / 8f);
            Color body = new Color(0.3f, 0.3f, 0.34f) + new Color(0.5f, 0.3f, 0.1f) * hot;
            Color glow = new Color(1f, 0.45f, 0.1f) * (hot * 0.9f);
            foreach (Renderer piece in _pickHead)
            {
                _kit.Paint(piece, body, glow);
            }
        }

        private void UpdateFlyers(float deltaTime)
        {
            for (int i = _flyers.Count - 1; i >= 0; i--)
            {
                Flyer flyer = _flyers[i];
                flyer.Progress += deltaTime / flyer.Duration;
                float u = Mathf.Clamp01(flyer.Progress);
                flyer.Transform.gameObject.SetActive(flyer.Progress >= 0f);
                flyer.Transform.localPosition = Vector3.Lerp(flyer.From, flyer.To, u) + Vector3.up * (flyer.Arc * 4f * u * (1f - u));
                flyer.Transform.Rotate(360f * deltaTime, 540f * deltaTime, 0f, Space.Self);
                if (flyer.Progress < 1f)
                {
                    continue;
                }

                NuggetLanded?.Invoke(flyer.To);
                UnityEngine.Object.Destroy(flyer.Transform.gameObject);
                _flyers.RemoveAt(i);
            }
        }

        private static float Range(System.Random random, float min, float max)
        {
            return min + (max - min) * (float)random.NextDouble();
        }
    }
}
