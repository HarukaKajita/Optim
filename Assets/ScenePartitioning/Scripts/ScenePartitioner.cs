using System;
using System.Collections.Generic;
using UnityEngine;

namespace Optim.ScenePartitioning
{
    /// <summary>
    /// Scene space partitioner that computes cells covering the entire scene.
    /// Supports grid and Voronoi based partitioning in 2D or 3D.
    /// </summary>
    [ExecuteAlways]
    public class ScenePartitioner : MonoBehaviour
    {
        public enum PartitionMethod
        {
            Grid2D,
            Grid3D,
            Voronoi2D,
            Voronoi3D
        }

        [Serializable]
        public class VoronoiSeed
        {
            public Vector3 position;
            public float weight = 1f;
        }

        [SerializeField]
        private PartitionMethod method = PartitionMethod.Grid2D;

        [SerializeField]
        private Vector3Int gridResolution = new Vector3Int(4, 1, 4);

        [SerializeField]
        private List<VoronoiSeed> voronoiSeeds = new List<VoronoiSeed>();

        [SerializeField]
        private float margin = 0f;

        private Bounds rootBounds;
        private int currentCellIndex = -1;
        private Vector3 lastCameraPosition = Vector3.positiveInfinity;
        private readonly Dictionary<int, Bounds> cells = new();

        public PartitionMethod Method
        {
            get => method;
            set { method = value; Recalculate(); }
        }

        public IReadOnlyDictionary<int, Bounds> Cells => cells;

        public event Action<int, int> OnCellChanged;

        private void OnEnable()
        {
            Recalculate();
        }

        private void Update()
        {
            Camera cam = Camera.main;
            if (cam == null)
                return;

            Vector3 pos = cam.transform.position;
            if (pos == lastCameraPosition)
                return;
            lastCameraPosition = pos;

            int index = GetCellIndex(pos);
            if (index != currentCellIndex)
            {
                int prev = currentCellIndex;
                currentCellIndex = index;
                OnCellChanged?.Invoke(prev, currentCellIndex);
            }
        }

        /// <summary>
        /// Recalculate root bounds and cell layout.
        /// </summary>
        [ContextMenu("Recalculate Partitions")]
        public void Recalculate()
        {
            CollectBounds();
            BuildCells();
        }

        private void CollectBounds()
        {
            Renderer[] renderers = FindObjectsOfType<Renderer>();
            if (renderers.Length == 0)
            {
                rootBounds = new Bounds(Vector3.zero, Vector3.one);
                return;
            }
            rootBounds = renderers[0].bounds;
            foreach (var r in renderers)
                rootBounds.Encapsulate(r.bounds);
            rootBounds.Expand(margin);
        }

        private void BuildCells()
        {
            cells.Clear();
            if (method == PartitionMethod.Grid2D || method == PartitionMethod.Grid3D)
                BuildGridCells();
            else
                BuildVoronoiCells();
        }

        private void BuildGridCells()
        {
            Vector3 size = rootBounds.size;
            Vector3Int res = method == PartitionMethod.Grid2D ? new Vector3Int(gridResolution.x, 1, gridResolution.z) : gridResolution;
            Vector3 cellSize = new Vector3(size.x / res.x, size.y / res.y, size.z / res.z);

            int index = 0;
            for (int x = 0; x < res.x; ++x)
            {
                for (int y = 0; y < res.y; ++y)
                {
                    for (int z = 0; z < res.z; ++z)
                    {
                        Vector3 min = rootBounds.min + Vector3.Scale(new Vector3(x, y, z), cellSize);
                        Bounds b = new Bounds(min + cellSize * 0.5f, cellSize);
                        cells[index++] = b;
                    }
                }
            }
        }

        private void BuildVoronoiCells()
        {
            if (voronoiSeeds.Count == 0)
                return;

            // Cells are implicit; store seed indices as cell ids.
            for (int i = 0; i < voronoiSeeds.Count; ++i)
            {
                cells[i] = new Bounds(voronoiSeeds[i].position, Vector3.zero);
            }
        }

        public int GetCameraCellIndex(Camera cam)
        {
            if (cam == null)
                return -1;
            return GetCellIndex(cam.transform.position);
        }

        /// <summary>
        /// Determine which cell contains the position.
        /// </summary>
        public int GetCellIndex(Vector3 position)
        {
            return method switch
            {
                PartitionMethod.Grid2D => GetGridCellIndex(position, false),
                PartitionMethod.Grid3D => GetGridCellIndex(position, true),
                PartitionMethod.Voronoi2D => GetVoronoiCellIndex(position, false),
                PartitionMethod.Voronoi3D => GetVoronoiCellIndex(position, true),
                _ => -1
            };
        }

        private int GetGridCellIndex(Vector3 position, bool useY)
        {
            Vector3 size = rootBounds.size;
            Vector3Int res = useY ? gridResolution : new Vector3Int(gridResolution.x, 1, gridResolution.z);
            Vector3 cellSize = new Vector3(size.x / res.x, size.y / res.y, size.z / res.z);
            Vector3 offset = position - rootBounds.min;
            int x = Mathf.Clamp(Mathf.FloorToInt(offset.x / cellSize.x), 0, res.x - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(offset.y / cellSize.y), 0, res.y - 1);
            int z = Mathf.Clamp(Mathf.FloorToInt(offset.z / cellSize.z), 0, res.z - 1);
            return x * res.y * res.z + y * res.z + z;
        }

        private int GetVoronoiCellIndex(Vector3 position, bool useY)
        {
            if (voronoiSeeds.Count == 0)
                return -1;
            int best = 0;
            float bestDist = float.MaxValue;
            for (int i = 0; i < voronoiSeeds.Count; ++i)
            {
                Vector3 seedPos = voronoiSeeds[i].position;
                if (!useY)
                {
                    seedPos.y = position.y; // ignore Y axis
                }
                float dist = (position - seedPos).sqrMagnitude / Mathf.Max(voronoiSeeds[i].weight * voronoiSeeds[i].weight, 0.0001f);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = i;
                }
            }
            return best;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!UnityEditor.SessionState.GetBool("SP_ShowCells", true))
                return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(rootBounds.center, rootBounds.size);

            if (method == PartitionMethod.Grid2D || method == PartitionMethod.Grid3D)
            {
                foreach (var kv in cells)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireCube(kv.Value.center, kv.Value.size);
                }
            }
            else
            {
                Gizmos.color = Color.white;
                foreach (var seed in voronoiSeeds)
                {
                    Gizmos.DrawSphere(seed.position, 0.1f);
                }
            }
        }
#endif
    }
}

