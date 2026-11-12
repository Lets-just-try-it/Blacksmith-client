using System.Collections.Generic;
using Blacksmith.Core.Battle;
using UnityEngine;
using UnityEngine.Rendering;

namespace Blacksmith.Battle
{
    // 전투 설정의 스테이지 맵(전투 시드로 만든 것)에 바닥 메시를 깐다. 맵의 시작 지점이 이 오브젝트의 위치에 온다.
    // 시드가 같으면 모든 참가자에게 같은 맵이 나와 따로 동기화하지 않는다.
    // 플레이어가 걸어 다니는 맵(채집)은 충돌을 켜서 바닥과 가장자리 벽에 콜라이더를 만든다. 전투는 유닛이 물리를 쓰지 않아 끈다.
    public class StageMapView : MonoBehaviour
    {
        [SerializeField] private bool _addColliders;
        [Tooltip("가장자리 벽 높이(m). 보이지 않는 충돌 전용이다")]
        [SerializeField, Min(0.1f)] private float _wallHeight = 3f;

        private GameObject _floor;
        private float _cellSize = 1f;
        private MapPoint _start;

        public void Build(StageMap map, StageMapData data)
        {
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
            if (_addColliders)
            {
                _floor.AddComponent<MeshCollider>().sharedMesh = _floor.GetComponent<MeshFilter>().sharedMesh;
                _floor.AddComponent<MeshCollider>().sharedMesh = CreateWallMesh(map, data.CellSize, _wallHeight, offset);
            }
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

        // 바닥 칸과 바닥이 아닌 칸 사이마다 세운 벽이다. 어느 쪽에서 부딪혀도 막히게 양면으로 감는다.
        private static Mesh CreateWallMesh(StageMap map, float cellSize, float height, Vector3 offset)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int x = 0; x < map.Width; x++)
            {
                for (int z = 0; z < map.Height; z++)
                {
                    if (!map.IsFloor(x, z))
                    {
                        continue;
                    }

                    if (!map.IsFloor(x - 1, z)) AddWall(x, z, x, z + 1);
                    if (!map.IsFloor(x + 1, z)) AddWall(x + 1, z, x + 1, z + 1);
                    if (!map.IsFloor(x, z - 1)) AddWall(x, z, x + 1, z);
                    if (!map.IsFloor(x, z + 1)) AddWall(x, z + 1, x + 1, z + 1);
                }
            }

            var mesh = new Mesh { name = "StageWalls" };
            if (vertices.Count > ushort.MaxValue)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;

            void AddWall(int x0, int z0, int x1, int z1)
            {
                int start = vertices.Count;
                Vector3 a = new Vector3(x0 * cellSize, 0f, z0 * cellSize) + offset;
                Vector3 b = new Vector3(x1 * cellSize, 0f, z1 * cellSize) + offset;
                vertices.Add(a);
                vertices.Add(a + Vector3.up * height);
                vertices.Add(b + Vector3.up * height);
                vertices.Add(b);
                triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
                triangles.AddRange(new[] { start, start + 2, start + 1, start, start + 3, start + 2 });
            }
        }

        // 전투 좌표(BattleSpace 단위)를 월드 좌표로 바꾼다. 시작 지점이 이 오브젝트의 위치다.
        public Vector3 ToWorld(float x, float z)
        {
            float cells = 1f / BattleSpace.UnitsPerCell;
            return transform.TransformPoint((x * cells - _start.X) * _cellSize, 0f, (z * cells - _start.Z) * _cellSize);
        }

        public bool IsBuilt => _floor != null;

        // 바닥과 벽을 감싸는 월드 영역. 깔지 않았으면 빈 영역이다.
        public Bounds Bounds => _floor != null ? _floor.GetComponent<MeshRenderer>().bounds : default;

        public void Clear()
        {
            if (_floor == null)
            {
                return;
            }

            Mesh floorMesh = _floor.GetComponent<MeshFilter>().sharedMesh;
            foreach (MeshCollider collider in _floor.GetComponents<MeshCollider>())
            {
                if (collider.sharedMesh != floorMesh)
                {
                    Destroy(collider.sharedMesh);
                }
            }

            Destroy(floorMesh);

            Destroy(_floor);
            _floor = null;
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}
