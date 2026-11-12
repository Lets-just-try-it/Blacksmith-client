using System.Collections.Generic;
using Blacksmith.Core.Battle;
using UnityEngine;
using UnityEngine.Rendering;

namespace Blacksmith.Battle
{
    // 전투 시드로 스테이지 맵을 만들어 바닥 메시를 깐다. 맵의 시작 지점이 이 오브젝트의 위치에 온다.
    // 시드가 같으면 모든 참가자에게 같은 맵이 나와 따로 동기화하지 않는다. 표시 전용이라 전투 결과에는 영향이 없다.
    public class StageMapView : MonoBehaviour
    {
        [Tooltip("던전 데이터에 스테이지 맵이 없을 때 쓴다")]
        [SerializeField] private StageMapData _defaultData;

        private GameObject _floor;
        private float _cellSize = 1f;
        private MapPoint _start;

        // 몬스터 무리와 보스의 스폰 지점. 아직 전투가 쓰지 않아 씬 뷰 기즈모로만 보인다.
        public StageLayout Layout { get; private set; }

        public void Build(int seed, StageMapData data, int mobGroupCount, bool hasBoss)
        {
            data = data != null ? data : _defaultData;
            StageMap map = StageMapGenerator.Generate(seed, data.ToSettings());
            Layout = StageLayoutGenerator.Generate(map, seed, mobGroupCount, hasBoss, data.ToLayoutSettings());
            _cellSize = data.CellSize;
            _start = map.Start;
            Clear();
            if (data.FloorMaterial == null)
            {
                Debug.LogWarning("스테이지 맵 데이터에 바닥 머티리얼이 없다.", data);
                return;
            }

            var offset = new Vector3(-map.Start.X * data.CellSize, 0f, -map.Start.Z * data.CellSize);
            _floor = new GameObject("Floor", typeof(MeshFilter), typeof(MeshRenderer));
            _floor.transform.SetParent(transform, false);
            _floor.GetComponent<MeshFilter>().sharedMesh = CreateMesh(map, data.CellSize, data.TextureWorldSize, offset);
            _floor.GetComponent<MeshRenderer>().sharedMaterial = data.FloorMaterial;
        }

        // 바닥 칸마다 사각형 하나를 깐다. 꼭짓점은 정수 칸 좌표로만 계산해 이웃 칸과 틈 없이 맞붙는다.
        // 위에서 볼 때 시계 방향으로 감아 윗면이 앞면이 된다. UV는 월드 좌표라 칸이 바뀌어도 텍스처가 이어진다.
        private static Mesh CreateMesh(StageMap map, float cellSize, float textureSize, Vector3 offset)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            for (int x = 0; x < map.Width; x++)
            {
                for (int z = 0; z < map.Height; z++)
                {
                    if (!map.IsFloor(x, z))
                    {
                        continue;
                    }

                    int start = vertices.Count;
                    AddVertex(x, z);
                    AddVertex(x, z + 1);
                    AddVertex(x + 1, z + 1);
                    AddVertex(x + 1, z);
                    triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
                }
            }

            var mesh = new Mesh { name = "StageFloor" };
            if (vertices.Count > ushort.MaxValue)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;

            void AddVertex(int x, int z)
            {
                Vector3 position = new Vector3(x * cellSize, 0f, z * cellSize) + offset;
                vertices.Add(position);
                uvs.Add(new Vector2(position.x / textureSize, position.z / textureSize));
            }
        }

        // 맵 칸 좌표를 월드 좌표로 바꾼다. 시작 지점이 이 오브젝트의 위치다.
        public Vector3 ToWorld(MapPoint point)
        {
            return transform.TransformPoint((point.X - _start.X) * _cellSize, 0f, (point.Z - _start.Z) * _cellSize);
        }

        private void OnDrawGizmos()
        {
            if (Layout == null)
            {
                return;
            }

            Gizmos.color = Color.red;
            foreach (MapPoint group in Layout.MobGroups)
            {
                Gizmos.DrawWireSphere(ToWorld(group), 3f * _cellSize);
            }

            if (Layout.HasBoss)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(ToWorld(Layout.Boss), 5f * _cellSize);
            }
        }

        private void Clear()
        {
            if (_floor == null)
            {
                return;
            }

            Destroy(_floor.GetComponent<MeshFilter>().sharedMesh);
            Destroy(_floor);
            _floor = null;
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}
