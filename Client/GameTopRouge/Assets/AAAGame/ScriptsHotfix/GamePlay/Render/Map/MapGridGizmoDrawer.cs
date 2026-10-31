#if UNITY_EDITOR
using AAAGame.ScriptsHotfix.GamePlay.Logic.Map;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AAAGame.ScriptsHotfix.GamePlay.Render.Map
{
    /// <summary>在 Unity Scene 视图中绘制地图可通行格与阻挡格的调试覆盖层。</summary>
    public sealed class MapGridGizmoDrawer : MonoBehaviour
    {
        /// <summary>可通行网格的填充颜色及透明度。</summary>
        private static readonly Color WalkableFill = new Color(0f, 1f, 0f, 0.22f);
        /// <summary>阻挡网格的填充颜色及透明度。</summary>
        private static readonly Color BlockedFill = new Color(1f, 0f, 0f, 0.28f);
        /// <summary>可通行网格的边框颜色及透明度。</summary>
        private static readonly Color WalkableOutline = new Color(0f, 1f, 0f, 0.9f);
        /// <summary>阻挡网格的边框颜色及透明度。</summary>
        private static readonly Color BlockedOutline = new Color(1f, 0f, 0f, 0.9f);

        /// <summary>复用绘制单个网格单元所需的四个顶点缓存。</summary>
        private readonly Vector3[] _cellVertices = new Vector3[4];
        /// <summary>当前用于绘制网格的地图逻辑信息。</summary>
        private MapInfo _mapInfo;

        /// <summary>设置或清除当前调试覆盖层关联的地图数据。</summary>
        /// <param name="mapInfo">需要绘制的地图信息；传入空值时停止绘制。</param>
        public void SetMapInfo(MapInfo mapInfo)
        {
            _mapInfo = mapInfo;
        }

        /// <summary>在 Scene 视图中绘制可通行与阻挡网格。</summary>
        private void OnDrawGizmos()
        {
            if (_mapInfo?.grid == null) return;

            Color previousColor = Handles.color;
            CompareFunction previousZTest = Handles.zTest;
            Handles.zTest = CompareFunction.Always;

            try
            {
                for (int x = 0; x < _mapInfo.MapWidth(); x++)
                {
                    for (int y = 0; y < _mapInfo.MapHeight(); y++)
                    {
                        GridInfo cell = _mapInfo.grid[x, y];
                        bool walkable = cell.groundCanPass;
                        Color fillColor = walkable ? WalkableFill : BlockedFill;
                        Color outlineColor = walkable ? WalkableOutline : BlockedOutline;

                        BuildCellVertices(x, y);
                        Handles.color = fillColor;
                        Handles.DrawAAConvexPolygon(_cellVertices);

                        Handles.color = outlineColor;
                        for (int i = 0; i < _cellVertices.Length; i++)
                        {
                            Handles.DrawLine(_cellVertices[i], _cellVertices[(i + 1) % _cellVertices.Length]);
                        }
                    }
                }
            }
            finally
            {
                Handles.color = previousColor;
                Handles.zTest = previousZTest;
            }
        }

        /// <summary>按地图投影方式计算单个网格的四个世界坐标顶点。</summary>
        /// <param name="x">网格的横向索引。</param>
        /// <param name="y">网格的纵向索引。</param>
        private void BuildCellVertices(int x, int y)
        {
            Vector3 center = (Vector3)_mapInfo.GetWorldPosByGrid(new Unity.Mathematics.int2(x, y));
            float halfWidth = (float)_mapInfo.gridSize.x * 0.5f;
            float halfHeight = (float)_mapInfo.gridSize.y * 0.5f;

            if (_mapInfo.isometric)
            {
                _cellVertices[0] = new Vector3(center.x, center.y + halfHeight, center.z);
                _cellVertices[1] = new Vector3(center.x + halfWidth, center.y, center.z);
                _cellVertices[2] = new Vector3(center.x, center.y - halfHeight, center.z);
                _cellVertices[3] = new Vector3(center.x - halfWidth, center.y, center.z);
                return;
            }

            _cellVertices[0] = new Vector3(center.x - halfWidth, center.y - halfHeight, center.z);
            _cellVertices[1] = new Vector3(center.x + halfWidth, center.y - halfHeight, center.z);
            _cellVertices[2] = new Vector3(center.x + halfWidth, center.y + halfHeight, center.z);
            _cellVertices[3] = new Vector3(center.x - halfWidth, center.y + halfHeight, center.z);
        }
    }
}
#endif
