using AAAGame.ScriptsHotfix.GamePlay.Render.Entity;
using cfg.Map;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.GamePlay.Render.Map
{
    /// <summary>将固定地图物件配置应用到资源实体并绑定交互组件。</summary>
    public sealed class MapObjectEntity : EntityRender
    {
        /// <summary>固定地图物件创建时传入的摆放和地图数据。</summary>
        public sealed class Data
        {
            /// <summary>当前固定物件的地图摆放配置。</summary>
            public ObjectPlacement Placement { get; set; }
            /// <summary>当前固定物件的配置类型。</summary>
            public ObjectType Type { get; set; }
            /// <summary>固定物件所属地图编号。</summary>
            public int MapId { get; set; }
        }

        /// <summary>实体显示时应用地图变换并初始化固定物件交互标记。</summary>
        /// <param name="userData">实体资源加载时传入的摆放数据。</param>
        protected override void OnShow(object userData)
        {
            base.OnShow(userData);
            Data data = (userData as AssetInfo)?.UserData as Data;
            if (data?.Placement == null)
            {
                Debug.LogError("Map fixed object is missing placement data.");
                return;
            }

            ObjectPlacement placement = data.Placement;
            transform.position = new Vector3(placement.X, placement.Y, placement.Z);
            transform.rotation = Quaternion.Euler(0f, 0f, placement.RotationZ);
            transform.localScale = new Vector3(placement.ScaleX, placement.ScaleY, placement.ScaleZ);

            MapObjectMarker marker = GetComponent<MapObjectMarker>();
            if (marker == null) marker = gameObject.AddComponent<MapObjectMarker>();
            marker.instanceId = placement.InstanceId;
            marker.objectId = placement.ObjectId;
            marker.objectType = data.Type;
            marker.radius = placement.Radius;
            marker.mapId = data.MapId;
            marker.objectiveId = placement.ObjectiveId;

            if (data.Type == ObjectType.TaskMarker)
            {
                TaskMarkerInteraction interaction = GetComponent<TaskMarkerInteraction>();
                if (interaction == null) interaction = gameObject.AddComponent<TaskMarkerInteraction>();
                interaction.Configure(marker);
            }
        }
    }
}
