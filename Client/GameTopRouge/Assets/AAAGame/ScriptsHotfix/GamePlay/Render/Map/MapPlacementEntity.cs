using AAAGame.ScriptsHotfix.GamePlay.Render.Entity;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.GamePlay.Render.Map
{
    /// <summary>将 NPC 或传送点摆放数据应用到地图表现实体。</summary>
    public sealed class MapPlacementEntity : EntityRender
    {
        /// <summary>创建地图摆放表现实体时传入的位置和交互数据。</summary>
        public sealed class PlacementData
        {
            /// <summary>实体在地图中的世界坐标。</summary>
            public Vector3 Position { get; set; }
            /// <summary>实体关联的 NPC 编号；传送点不关联 NPC。</summary>
            public int NpcId { get; set; }
            /// <summary>传送点目标地图编号；NPC 不配置目标地图。</summary>
            public int TargetMapId { get; set; }
            /// <summary>当前实体所属地图编号。</summary>
            public int MapId { get; set; }
            /// <summary>玩家可以触发实体交互的范围半径，单位为 Unity 世界单位。</summary>
            public float Radius { get; set; }
            /// <summary>当前实体是否为传送点。</summary>
            public bool IsPortal { get; set; }
        }

        /// <summary>实体显示时绑定 NPC 或传送点标记并设置渲染层级。</summary>
        /// <param name="userData">实体资源加载时传入的摆放数据。</param>
        protected override void OnShow(object userData)
        {
            base.OnShow(userData);
            PlacementData data = (userData as AssetInfo)?.UserData as PlacementData;
            if (data == null)
            {
                Debug.LogError("Map placement entity is missing placement data.");
                return;
            }

            transform.position = data.Position;
            transform.rotation = Quaternion.identity;
            MapPlacementMarker marker = GetComponent<MapPlacementMarker>();
            if (marker == null) marker = gameObject.AddComponent<MapPlacementMarker>();
            marker.portal = data.IsPortal;
            marker.npcId = data.NpcId;
            marker.mapId = data.MapId;
            marker.radius = data.Radius;
            marker.targetMapId = data.TargetMapId;
            marker.targetMap = data.TargetMapId.ToString();

            if (!data.IsPortal)
            {
                NpcDialogueInteraction interaction = GetComponent<NpcDialogueInteraction>();
                if (interaction == null) interaction = gameObject.AddComponent<NpcDialogueInteraction>();
                interaction.Configure(marker);
            }

            MapRenderLayer renderLayer = data.IsPortal ? MapRenderLayer.Effect : MapRenderLayer.Role;
            foreach (SpriteRenderer sprite in GetComponentsInChildren<SpriteRenderer>(true))
                sprite.sortingOrder = (int)renderLayer;
        }
    }
}
