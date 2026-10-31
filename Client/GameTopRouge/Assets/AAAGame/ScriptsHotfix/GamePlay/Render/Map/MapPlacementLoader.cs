using System;
using System.Collections.Generic;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Common;
using cfg.Map;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.GamePlay.Render.Map
{
    /// <summary>加载并回收地图中的固定物件、NPC 和传送点。</summary>
    public sealed class MapPlacementLoader
    {
        /// <summary>表示固定物件未绑定调查目标的编号。</summary>
        private const int NoObjectiveId = 0;
        /// <summary>表示地图摆放数据未关联 NPC 的编号。</summary>
        private const int NoNpcId = 0;
        /// <summary>表示地图摆放数据未设置目标地图的编号。</summary>
        private const int NoTargetMapId = 0;
        /// <summary>当前加载批次创建或正在加载的实体编号。</summary>
        private readonly List<int> _entityIds = new List<int>();
        /// <summary>当前地图加载批次，用于丢弃过期的异步回调。</summary>
        private int _generation;

        /// <summary>停止并隐藏当前批次的地图实体。</summary>
        public void Clear()
        {
            _generation++;
            foreach (int entityId in _entityIds)
            {
                if (GF.Entity.HasEntity(entityId) || GF.Entity.IsLoadingEntity(entityId))
                    GF.Entity.HideEntity(entityId);
            }
            _entityIds.Clear();
        }

        /// <summary>读取当前地图所需配置并启动各类摆放实体加载。</summary>
        /// <param name="map">当前地图配置；为空时记录错误并结束加载。</param>
        /// <returns>配置读取和实体加载请求提交完成后的异步任务。</returns>
        public async UniTask LoadAsync(MapData map)
        {
            if (map == null)
            {
                Debug.LogError("Cannot load placements for a null map.");
                return;
            }

            Clear();
            int generation = _generation;
            try
            {
                TbMapObject objectDefinitions = await GF.LuBan.Get<TbMapObject>(cfg.Tables.map_tbmapobject);
                if (!IsCurrentGeneration(generation)) return;
                LoadObjects(map, objectDefinitions, generation);

                TbNpc npcDefinitions = await GF.LuBan.Get<TbNpc>(cfg.Tables.map_tbnpc);
                if (!IsCurrentGeneration(generation)) return;
                LoadNpcs(map, npcDefinitions, generation);

                TbMapData mapDefinitions = await GF.LuBan.Get<TbMapData>(cfg.Tables.map_tbmapdata);
                if (!IsCurrentGeneration(generation)) return;
                LoadPortals(map, mapDefinitions, generation);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        /// <summary>验证并创建地图中配置的固定物件。</summary>
        /// <param name="map">当前地图配置。</param>
        /// <param name="definitions">按固定物件编号索引的配置表。</param>
        /// <param name="generation">发起本次加载的地图批次编号。</param>
        private void LoadObjects(MapData map, TbMapObject definitions, int generation)
        {
            var instanceIds = new HashSet<int>();
            foreach (ObjectPlacement placement in map.Objects)
            {
                MapObject definition = placement == null ? null : definitions.GetOrDefault(placement.ObjectId);
                if (!IsValidObjectPlacement(placement, definition, instanceIds, out string error))
                {
                    Debug.LogError($"Map {map.Id} has an invalid fixed object placement: {error}");
                    continue;
                }

                var userData = new MapObjectEntity.Data
                {
                    Placement = placement,
                    Type = definition.Type,
                    MapId = map.Id
                };
                int entityId = GF.AssetBridge.LoadEntity(definition.Res, typeof(MapObjectEntity), "Map",
                    GamePlayDefine.LoadPriority.Aoe,
                    info => OnObjectLoaded(info, map.Id, placement.InstanceId, definition.Res, generation), userData);
                TrackEntity(entityId, map.Id, placement.InstanceId, definition.Res);
            }
        }

        /// <summary>验证固定物件引用、唯一编号、目标绑定和空间参数。</summary>
        /// <param name="placement">地图中的固定物件摆放实例。</param>
        /// <param name="definition">固定物件类型和资源配置。</param>
        /// <param name="instanceIds">当前地图已经出现的实例编号集合。</param>
        /// <param name="error">验证失败时返回的原因。</param>
        /// <returns>配置满足地图运行要求时返回真。</returns>
        private static bool IsValidObjectPlacement(ObjectPlacement placement, MapObject definition,
            HashSet<int> instanceIds, out string error)
        {
            if (placement == null)
            {
                error = "placement is missing";
                return false;
            }
            if (definition == null)
            {
                error = $"object {placement.ObjectId} is missing";
                return false;
            }
            if (placement.InstanceId <= 0 || !instanceIds.Add(placement.InstanceId))
            {
                error = $"instance id {placement.InstanceId} is invalid or duplicated";
                return false;
            }
            if (definition.Type != ObjectType.TaskMarker && definition.Type != ObjectType.SceneProp)
            {
                error = $"object {definition.Id} has unsupported type {definition.Type}";
                return false;
            }
            if (string.IsNullOrWhiteSpace(definition.Res))
            {
                error = $"object {definition.Id} has no prefab resource";
                return false;
            }
            if (definition.Type == ObjectType.TaskMarker && placement.ObjectiveId <= NoObjectiveId)
            {
                error = $"task marker {placement.InstanceId} has no objective";
                return false;
            }
            if (definition.Type == ObjectType.SceneProp && placement.ObjectiveId != NoObjectiveId)
            {
                error = $"scene prop {placement.InstanceId} cannot bind an objective";
                return false;
            }
            if (!IsFinite(placement.X) || !IsFinite(placement.Y) || !IsFinite(placement.Z) ||
                !IsFinite(placement.RotationZ) || !IsFinite(placement.ScaleX) || !IsFinite(placement.ScaleY) ||
                !IsFinite(placement.ScaleZ) || !IsFinite(placement.Radius))
            {
                error = $"instance {placement.InstanceId} has a non-finite transform or radius";
                return false;
            }
            if (placement.ScaleX <= 0 || placement.ScaleY <= 0 || placement.ScaleZ <= 0 || placement.Radius <= 0)
            {
                error = $"instance {placement.InstanceId} has a non-positive scale or radius";
                return false;
            }

            error = string.Empty;
            return true;
        }

        /// <summary>验证并创建地图中配置的 NPC。</summary>
        /// <param name="map">当前地图配置。</param>
        /// <param name="definitions">按 NPC 编号索引的配置表。</param>
        /// <param name="generation">发起本次加载的地图批次编号。</param>
        private void LoadNpcs(MapData map, TbNpc definitions, int generation)
        {
            foreach (var placement in map.Npc)
            {
                Npc definition = definitions.GetOrDefault(placement.NpcId);
                if (definition == null || string.IsNullOrWhiteSpace(definition.Res) ||
                    !IsFinite(placement.X) || !IsFinite(placement.Y) || !IsFinite(placement.Z) ||
                    !IsFinite(placement.Radius) || placement.Radius <= 0)
                {
                    Debug.LogError($"Map {map.Id} has an invalid NPC placement for NPC {placement.NpcId}.");
                    continue;
                }

                Spawn(definition.Res, new Vector3(placement.X, placement.Y, placement.Z), generation,
                    placement.NpcId, placement.Radius, NoTargetMapId, map.Id, false);
            }
        }

        /// <summary>验证并创建地图中配置的传送点。</summary>
        /// <param name="map">当前地图配置。</param>
        /// <param name="definitions">按地图编号索引的地图配置表。</param>
        /// <param name="generation">发起本次加载的地图批次编号。</param>
        private void LoadPortals(MapData map, TbMapData definitions, int generation)
        {
            foreach (var placement in map.Portal)
            {
                bool destinationExists = definitions.GetOrDefault(placement.TargetMapId) != null;
                if (!destinationExists || !IsFinite(placement.X) || !IsFinite(placement.Y) ||
                    !IsFinite(placement.Z) || !IsFinite(placement.Radius) || placement.Radius <= 0)
                {
                    Debug.LogError($"Map {map.Id} has an invalid portal destination {placement.TargetMapId}.");
                    continue;
                }

                Spawn(MapPlacementMarker.PortalPrefab, new Vector3(placement.X, placement.Y, placement.Z),
                    generation, NoNpcId, placement.Radius, placement.TargetMapId, map.Id, true);
            }
        }

        /// <summary>加载 NPC 或传送点表现实体并登记其所属地图批次。</summary>
        /// <param name="resourcePath">表现实体预制体资源路径。</param>
        /// <param name="position">实体在地图中的世界坐标。</param>
        /// <param name="generation">发起本次加载的地图批次编号。</param>
        /// <param name="npcId">关联 NPC 编号；传送点使用零。</param>
        /// <param name="radius">玩家触发交互的范围半径。</param>
        /// <param name="targetMapId">目标地图编号；NPC 使用零。</param>
        /// <param name="mapId">当前地图编号。</param>
        /// <param name="isPortal">实体是否为传送点。</param>
        private void Spawn(string resourcePath, Vector3 position, int generation, int npcId, float radius,
            int targetMapId, int mapId, bool isPortal)
        {
            var userData = new MapPlacementEntity.PlacementData
            {
                Position = position,
                NpcId = npcId,
                Radius = radius,
                TargetMapId = targetMapId,
                MapId = mapId,
                IsPortal = isPortal
            };
            int entityId = GF.AssetBridge.LoadEntity(resourcePath, typeof(MapPlacementEntity), "Map",
                GamePlayDefine.LoadPriority.Aoe,
                info => OnPlacementLoaded(info, mapId, isPortal ? targetMapId : npcId, resourcePath, generation), userData);
            TrackEntity(entityId, mapId, npcId, resourcePath);
        }

        /// <summary>记录已成功提交给实体系统的加载编号。</summary>
        /// <param name="entityId">实体系统返回的加载编号。</param>
        /// <param name="mapId">该实体所属地图编号。</param>
        /// <param name="sourceId">对应的摆放实例编号或 NPC 编号。</param>
        /// <param name="resourcePath">实体资源路径。</param>
        private void TrackEntity(int entityId, int mapId, int sourceId, string resourcePath)
        {
            if (entityId >= 0)
            {
                _entityIds.Add(entityId);
                return;
            }
            Debug.LogError($"Map {mapId}, placement {sourceId}, resource {resourcePath}: entity load was rejected.");
        }

        /// <summary>处理固定物件实体加载完成回调，并隐藏已经过期的实体。</summary>
        /// <param name="info">实体系统返回的加载结果。</param>
        /// <param name="mapId">固定物件所属地图编号。</param>
        /// <param name="instanceId">固定物件摆放实例编号。</param>
        /// <param name="resourcePath">固定物件资源路径。</param>
        /// <param name="generation">发起加载的地图批次编号。</param>
        private void OnObjectLoaded(AssetInfo info, int mapId, int instanceId, string resourcePath, int generation)
        {
            var entity = info.Asset as UnityGameFramework.Runtime.Entity;
            if (entity == null)
            {
                Debug.LogError($"Map {mapId}, instance {instanceId}, resource {resourcePath}: entity is missing.");
                return;
            }
            if (!IsCurrentGeneration(generation)) GF.AssetBridge.HideEntity(entity);
        }

        /// <summary>处理 NPC 或传送点实体加载完成回调，并隐藏已经过期的实体。</summary>
        /// <param name="info">实体系统返回的加载结果。</param>
        /// <param name="mapId">地图摆放实体所属地图编号。</param>
        /// <param name="sourceId">对应 NPC 编号或传送点目标地图编号。</param>
        /// <param name="resourcePath">表现实体资源路径。</param>
        /// <param name="generation">发起加载的地图批次编号。</param>
        private void OnPlacementLoaded(AssetInfo info, int mapId, int sourceId, string resourcePath, int generation)
        {
            var entity = info.Asset as UnityGameFramework.Runtime.Entity;
            if (entity == null)
            {
                Debug.LogError($"Map {mapId}, placement {sourceId}, resource {resourcePath}: entity is missing.");
                return;
            }
            if (!IsCurrentGeneration(generation)) GF.AssetBridge.HideEntity(entity);
        }

        /// <summary>检查回调是否仍属于当前地图加载批次。</summary>
        /// <param name="generation">回调携带的地图批次编号。</param>
        /// <returns>编号与当前批次相同时返回真。</returns>
        private bool IsCurrentGeneration(int generation)
        {
            return generation == _generation;
        }

        /// <summary>判断数值是否可安全用于地图坐标或范围。</summary>
        /// <param name="value">待检查的地图数值。</param>
        /// <returns>数值不是 NaN 且不是无穷大时返回真。</returns>
        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

    }
}
