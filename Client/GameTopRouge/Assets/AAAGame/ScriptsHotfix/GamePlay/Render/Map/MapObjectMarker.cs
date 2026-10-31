using cfg.Map;
using UnityEngine;

namespace AAAGame.ScriptsHotfix.GamePlay.Render.Map
{
    /// <summary>保存固定地图物件的摆放编号、类型和任务目标绑定。</summary>
    [DisallowMultipleComponent]
    public sealed class MapObjectMarker : MonoBehaviour
    {
        /// <summary>绘制交互范围圆环时使用的线段数。</summary>
        private const int GizmoSegmentCount = 64;
        /// <summary>交互提示尚未绑定地图数据时使用的默认半径，单位为世界单位。</summary>
        private const float DefaultInteractionRadius = 1f;
        /// <summary>圆形范围的弧度总量。</summary>
        private const float FullCircleRadians = 6.283185f;

        /// <summary>地图编辑器中此物件摆放实例的唯一编号。</summary>
        public int instanceId;
        /// <summary>固定物件配置编号。</summary>
        public int objectId;
        /// <summary>该物件所属地图配置编号。</summary>
        public int mapId;
        /// <summary>任务物件绑定的目标编号；非任务物件为零。</summary>
        public int objectiveId;
        /// <summary>固定物件配置的场景物件类型。</summary>
        public ObjectType objectType;
        /// <summary>玩家可触发该物件交互的范围半径，单位为世界单位；默认值为一。</summary>
        public float radius = DefaultInteractionRadius;

        /// <summary>选中物件时在 Scene 视图绘制其交互范围。</summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
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
