using System.Collections.Generic;
using Blacksmith.Core.Forge;
using UnityEngine;

namespace Blacksmith.Forge
{
    // ① 쪼개기의 균열 표시. 균열마다 중심 표시와 줄어드는 바깥 고리를 원석 앞 좌표 평면에 그린다.
    // 고리가 중심에 닿는 순간이 곡괭이를 내리찍을 때다. 주황은 균열, 보라는 저주받은 균열이다.
    // 결과가 정해지면 고리가 터지듯 퍼지며 사라진다. 균열이 겹치지 않으므로 한 번에 보이는 수만큼만 칸을 둔다.
    public class SmeltingCrackMarkers
    {
        private const int SlotCount = 10;
        private const int RingPoints = 40;
        private const float ResultTime = 0.45f;
        // 줄어들기 시작할 때 바깥 고리가 중심 표시의 몇 배 크기인지
        private const float OuterScale = 2.6f;
        // 표시 조각이 같은 평면에서 깜빡이지 않도록, 카메라 쪽(-z)으로 아주 조금씩 층을 나눠 띄운다. 값이 작을수록 뒤다.
        // 많이 띄우면 화면 가장자리에서 조각들이 어긋나 보이므로 겹치지 않을 만큼만 띄운다.
        private const float DiscDepth = 0.02f;
        private const float DotDepth = 0f;
        private const float LineDepth = -0.03f;
        private const float RingDepth = -0.06f;
        private const float OuterDepth = -0.09f;
        private static readonly Color VeinColor = new Color(1f, 0.62f, 0.2f);
        private static readonly Color CurseColor = new Color(0.78f, 0.35f, 0.95f);
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        private sealed class Slot
        {
            public Transform Root;
            public Transform Disc;
            public Renderer DiscRenderer;
            public Transform Dot;
            public Renderer DotRenderer;
            public LineRenderer Ring;
            public LineRenderer Outer;
            public LineRenderer CrossA;
            public LineRenderer CrossB;
            public int Target = -1;
        }

        private readonly SmeltingGreybox _kit;
        private readonly Slot[] _slots = new Slot[SlotCount];
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private float[] _resultAt = new float[0];
        private float[] _resultQuality = new float[0];
        private bool _active;

        public SmeltingCrackMarkers(SmeltingGreybox kit, Transform stage, Material lineMaterial)
        {
            _kit = kit;
            Quaternion facing = Quaternion.LookRotation(-SmeltingLayout.ArenaNormal, SmeltingLayout.ArenaUp);
            for (int i = 0; i < SlotCount; i++)
            {
                var root = new GameObject("CrackMarker" + i).transform;
                root.SetParent(stage, false);
                root.localRotation = facing;
                var slot = new Slot { Root = root };
                // 원통은 위쪽(y)이 축이라, 평면(z가 법선)에 눕히려고 90도 돌려 둔다.
                GameObject disc = kit.Cylinder(root, "Disc", new Vector3(0f, 0f, DiscDepth), new Vector3(1f, 0.005f, 1f), Color.black);
                disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                slot.Disc = disc.transform;
                slot.DiscRenderer = disc.GetComponent<Renderer>();
                GameObject dot = kit.Cylinder(root, "Dot", new Vector3(0f, 0f, DotDepth), new Vector3(1f, 0.005f, 1f), Color.black);
                dot.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                slot.Dot = dot.transform;
                slot.DotRenderer = dot.GetComponent<Renderer>();
                slot.Ring = NewLine(root, "Ring", lineMaterial, RingPoints + 1, true);
                slot.Outer = NewLine(root, "Outer", lineMaterial, RingPoints + 1, true);
                slot.CrossA = NewLine(root, "CrossA", lineMaterial, 2, false);
                slot.CrossB = NewLine(root, "CrossB", lineMaterial, 2, false);
                _slots[i] = slot;
                root.gameObject.SetActive(false);
            }
        }

        public void Bind(SmeltingGame game)
        {
            _resultAt = new float[game.Targets.Count];
            _resultQuality = new float[game.Targets.Count];
            foreach (Slot slot in _slots)
            {
                slot.Target = -1;
                slot.Root.gameObject.SetActive(false);
            }
        }

        public void Hide()
        {
            _active = false;
            foreach (Slot slot in _slots)
            {
                slot.Target = -1;
                slot.Root.gameObject.SetActive(false);
            }
        }

        // 결과: 1 완벽, 0.75 좋음, 0.4 약함, 0 놓침, -1 저주를 누름, -2 저주를 넘김
        public void MarkResult(int index, float quality)
        {
            if (index >= 0 && index < _resultAt.Length)
            {
                _resultAt[index] = Time.time;
                _resultQuality[index] = quality;
            }
        }

        public void Tick(SmeltingGame game)
        {
            float approach = game.Approach;
            _active = game.Act == SmeltingGame.ActKind.Crack;
            if (!_active)
            {
                Hide();
                return;
            }

            float baseRadius = game.HitRadius * SmeltingLayout.ArenaUnit * 0.95f;
            ReleaseFinishedSlots(game, approach);
            for (int i = 0; i < game.Targets.Count; i++)
            {
                if (!IsShown(game, i, approach) || FindSlot(i) != null)
                {
                    continue;
                }

                Slot free = FindSlot(-1);
                if (free == null)
                {
                    break;
                }

                SmeltingGame.Target target = game.Targets[i];
                free.Target = i;
                free.Root.localPosition = SmeltingLayout.ArenaToLocal(target.X, target.Y);
                free.Root.gameObject.SetActive(true);
            }

            foreach (Slot slot in _slots)
            {
                if (slot.Target >= 0)
                {
                    Draw(slot, game, approach, baseRadius);
                }
            }
        }

        private bool IsShown(SmeltingGame game, int index, float approach)
        {
            SmeltingGame.Target target = game.Targets[index];
            if (target.State == SmeltingGame.TargetState.Pending)
            {
                return target.Time - game.ActClock <= approach;
            }
            return Time.time - _resultAt[index] < ResultTime;
        }

        private void ReleaseFinishedSlots(SmeltingGame game, float approach)
        {
            foreach (Slot slot in _slots)
            {
                if (slot.Target >= 0 && !IsShown(game, slot.Target, approach))
                {
                    slot.Target = -1;
                    slot.Root.gameObject.SetActive(false);
                }
            }
        }

        private Slot FindSlot(int target)
        {
            foreach (Slot slot in _slots)
            {
                if (slot.Target == target)
                {
                    return slot;
                }
            }
            return null;
        }

        private void Draw(Slot slot, SmeltingGame game, float approach, float baseRadius)
        {
            SmeltingGame.Target target = game.Targets[slot.Target];
            bool curse = target.Kind == SmeltingGame.TargetKind.Decoy;
            Color color = curse ? CurseColor : VeinColor;
            if (target.State == SmeltingGame.TargetState.Pending)
            {
                DrawPending(slot, game, target, color, curse, approach, baseRadius);
            }
            else
            {
                DrawResult(slot, color, baseRadius);
            }
        }

        private void DrawPending(Slot slot, SmeltingGame game, SmeltingGame.Target target, Color color, bool curse, float approach, float baseRadius)
        {
            float until = target.Time - game.ActClock;
            float appear = Mathf.Clamp01((approach - until) / 0.18f);
            float pop = (0.6f + 0.4f * Mathf.Sin(appear * Mathf.PI * 0.5f)) * appear;
            float outerRadius = Mathf.Lerp(baseRadius, baseRadius * OuterScale, Mathf.Clamp01(until / approach)) * appear;

            SetDisc(slot.Disc, slot.DiscRenderer, baseRadius * pop, color * 0.25f, Color.black);
            SetRing(slot.Ring, baseRadius * pop, 0.04f, color, RingDepth);
            slot.Dot.gameObject.SetActive(!curse);
            slot.CrossA.gameObject.SetActive(curse);
            slot.CrossB.gameObject.SetActive(curse);
            if (curse)
            {
                float k = baseRadius * 0.45f * pop;
                SetLine(slot.CrossA, new Vector3(-k, -k, LineDepth), new Vector3(k, k, LineDepth), 0.06f, color);
                SetLine(slot.CrossB, new Vector3(-k, k, LineDepth), new Vector3(k, -k, LineDepth), 0.06f, color);
            }
            else
            {
                SetDisc(slot.Dot, slot.DotRenderer, baseRadius * 0.28f * pop * 2f, color, color * 0.9f);
            }

            // 줄어드는 바깥 고리는 닿기 직전에 하얗게 변한다.
            float near = 1f - Mathf.Clamp01(Mathf.Abs(until) / 0.25f);
            SetRing(slot.Outer, outerRadius, 0.06f, Color.Lerp(color, Color.white, near), OuterDepth);
        }

        // 결과: 터지며 사라진다.
        private void DrawResult(Slot slot, Color fallback, float baseRadius)
        {
            float age = Mathf.Clamp01((Time.time - _resultAt[slot.Target]) / ResultTime);
            float quality = _resultQuality[slot.Target];
            Color color = quality >= 1f ? new Color(1f, 0.88f, 0.3f)
                : (quality >= 0.75f ? new Color(0.5f, 0.92f, 0.45f)
                : (quality > 0f ? new Color(0.85f, 0.85f, 0.88f)
                : (quality == -2f ? new Color(0.75f, 0.55f, 1f) : new Color(1f, 0.35f, 0.3f))));
            slot.Dot.gameObject.SetActive(false);
            slot.CrossA.gameObject.SetActive(false);
            slot.CrossB.gameObject.SetActive(false);

            SetRing(slot.Ring, baseRadius * (1f + 0.9f * age), 0.04f * (1f - age), color, RingDepth);
            if (quality > 0f)
            {
                SetRing(slot.Outer, baseRadius * (1.2f + 1.8f * age), 0.03f * (1f - age), color, OuterDepth);
            }
            else
            {
                slot.Outer.gameObject.SetActive(false);
            }

            if (quality == -1f || quality == 0f)
            {
                SetDisc(slot.Disc, slot.DiscRenderer, baseRadius * (1f - age * 0.5f), new Color(0.5f, 0.15f, 0.15f), Color.black);
            }
            else
            {
                slot.Disc.localScale = Vector3.zero;
            }
        }

        private void SetDisc(Transform disc, Renderer renderer, float radius, Color color, Color emission)
        {
            disc.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);
            _kit.Paint(renderer, color, emission);
        }

        private void SetRing(LineRenderer line, float radius, float width, Color color, float depth)
        {
            line.gameObject.SetActive(width > 0.001f && radius > 0.001f);
            if (!line.gameObject.activeSelf)
            {
                return;
            }

            for (int i = 0; i <= RingPoints; i++)
            {
                float angle = i / (float)RingPoints * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, depth));
            }
            Style(line, width, color);
        }

        private void SetLine(LineRenderer line, Vector3 from, Vector3 to, float width, Color color)
        {
            line.SetPosition(0, from);
            line.SetPosition(1, to);
            Style(line, width, color);
        }

        private void Style(LineRenderer line, float width, Color color)
        {
            line.widthMultiplier = width;
            line.GetPropertyBlock(_block);
            _block.SetColor(BaseColor, color);
            line.SetPropertyBlock(_block);
        }

        private static LineRenderer NewLine(Transform parent, string name, Material material, int points, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = loop;
            line.positionCount = points;
            line.sharedMaterial = material;
            line.numCapVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.alignment = LineAlignment.TransformZ;
            return line;
        }
    }
}
