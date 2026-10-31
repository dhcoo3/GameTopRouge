using UnityEngine;

namespace AAAGame.ScriptsHotfix.GamePlay.Render.Map
{
    /// <summary>保存地图中 NPC 或传送点实例的交互数据。</summary>
    [DisallowMultipleComponent]
    public sealed class MapPlacementMarker : MonoBehaviour
    {
        /// <summary>地图传送点使用的表现预制体路径。</summary>
        public const string PortalPrefab = "Assets/AAAGame/Prefabs/Effect/Portal.prefab";
        /// <summary>绘制交互范围圆环时使用的线段数。</summary>
        private const int GizmoSegmentCount = 64;
        /// <summary>交互提示尚未绑定地图数据时使用的默认半径，单位为世界单位。</summary>
        private const float DefaultInteractionRadius = 1f;
        /// <summary>圆形范围的弧度总量。</summary>
        private const float FullCircleRadians = 6.283185f;

        /// <summary>当前摆放实例是否为传送点。</summary>
        public bool portal;
        /// <summary>关联的 NPC 编号；传送点不关联 NPC。</summary>
        public int npcId;
        /// <summary>该实例所属地图配置编号。</summary>
        public int mapId;
        /// <summary>玩家可触发该实例交互的范围半径，单位为世界单位；默认值为一。</summary>
        public float radius = DefaultInteractionRadius;
        /// <summary>传送点目标地图的稳定名称、标题或场景标识。</summary>
        public string targetMap;
        /// <summary>传送点目标地图配置编号。</summary>
        public int targetMapId;

        /// <summary>选中实例时在 Scene 视图绘制其交互范围。</summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector3 center = transform.position;
            Vector3 previous = center + Vector3.right * radius;
            for (int segment = 1; segment <= GizmoSegmentCount; segment++)
            {
                float angle = segment * FullCircleRadians / GizmoSegmentCount;
                Vector3 next = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }
    }
}
