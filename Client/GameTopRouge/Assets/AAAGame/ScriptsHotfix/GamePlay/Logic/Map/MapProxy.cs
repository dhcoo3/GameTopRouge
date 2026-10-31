using System;
using System.Collections.Generic;
using System.Globalization;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Common;
using AAAGame.ScriptsHotfix.GamePlay.Logic.ProxyManager;
using cfg.Map;
using Unity.Mathematics;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Map
{
    public class MapProxy:GameProxy
    {
        private MapInfo _mapInfo;
        public MapInfo MapInfo => _mapInfo;
        
        private MapData curMapData;
        
        private MapAStar _mapAStar;

        private int2[] _heroSpawnPoints;

        private TbMapData _TbMapData;
        
        public override async void Initialize()
        {
            try
            {
                _TbMapData = await GF.LuBan.Get<TbMapData>(cfg.Tables.map_tbmapdata);
            }
            catch (Exception e)
            {
                Log.Error("Initialize error = {0}",e.Message);
            }
        }

        public override void LogicUpdate(fix deltaTime)
        {
            _mapAStar?.LogicUpdate(deltaTime);
            base.LogicUpdate(deltaTime);
        }

        public void LoadMap(int mapId)
        {
            _heroSpawnPoints = null;
            MapData mapData = _TbMapData.GetOrDefault(mapId);
            
            if (mapData == null)
            {
                Log.Error("不存在地图数据 {0}",mapId);
                return;
            }

            curMapData = mapData;
            
            if (curMapData.Random)
            {
                RandomMap(curMapData.Width,curMapData.Height,0);
            }
            else
            {
                CreateSceneMap(curMapData);
            }

            LoadHeroSpawnPoints(curMapData.HeroSpawnPoints);
          
            Fire(GamePlayEvent.ECreateMapRender,curMapData);
        }

        //随机创建一个地图
        public void RandomMap(int mapWidth, int mapHeight, fix waterline){
            _mapAStar?.Dispose();
            GridInfo grass = new GridInfo("Grass");
            GridInfo water = new GridInfo("Water", false);
            GridInfo[,] mGrids = new GridInfo[mapWidth, mapHeight];
            
            for (var i = 0; i < mapWidth; i++){
                for (var j = 0; j < mapHeight; j++){
                    mGrids[i, j] = grass;
                }
            }
     
            _mapInfo = new MapInfo(mGrids, new fix2(1,1));
            _mapAStar = MapAStar.Create(_mapInfo);
        }

        private void LoadHeroSpawnPoints(string spawnPointData)
        {
            if (_mapInfo == null || string.IsNullOrWhiteSpace(spawnPointData))
            {
                Log.Error("地图英雄出生点未配置。");
                return;
            }

            string[] entries = spawnPointData.Split(';');
            if (entries.Length != 4)
            {
                Log.Error("地图英雄出生点必须配置4个，当前数量={0}", entries.Length);
                return;
            }

            int2[] points = new int2[4];
            for (int index = 0; index < entries.Length; index++)
            {
                string[] coordinates = entries[index].Split(',');
                if (coordinates.Length != 2 ||
                    !int.TryParse(coordinates[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int x) ||
                    !int.TryParse(coordinates[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int y))
                {
                    Log.Error("地图英雄出生点格式无效，序号={0}，内容={1}", index, entries[index]);
                    return;
                }

                int2 point = new int2(x, y);
                if (!_mapInfo.CanGridPasses(x, y, MoveType.ground, false))
                {
                    Log.Error("地图英雄出生点越界或位于阻挡格，序号={0}，坐标={1},{2}", index, x, y);
                    return;
                }

                for (int previous = 0; previous < index; previous++)
                {
                    if (points[previous].Equals(point))
                    {
                        Log.Error("地图英雄出生点不能重复，坐标={0},{1}", x, y);
                        return;
                    }
                }

                points[index] = point;
            }

            _heroSpawnPoints = points;
        }

        public bool TryGetHeroSpawnWorldPosition(int spawnIndex, out fix3 worldPosition)
        {
            worldPosition = fix3.zero;
            if (spawnIndex < 0 || spawnIndex >= 4)
            {
                Log.Error("玩家出生位索引必须在0到3之间，当前值={0}", spawnIndex);
                return false;
            }

            if (_mapInfo == null || _heroSpawnPoints == null || _heroSpawnPoints.Length != 4)
            {
                return false;
            }

            worldPosition = _mapInfo.GetWorldPosByGrid(_heroSpawnPoints[spawnIndex]);
            return true;
        }

        private void CreateSceneMap(MapData mapData)
        {
            _mapAStar?.Dispose();
            GridInfo[,] grids = new GridInfo[mapData.Width, mapData.Height];
            GridInfo grass = new GridInfo("Grass");
            for (int x = 0; x < mapData.Width; x++)
            {
                for (int y = 0; y < mapData.Height; y++)
                {
                    grids[x, y] = grass;
                }
            }

            if (!string.IsNullOrEmpty(mapData.BlockedCells))
            {
                string[] cells = mapData.BlockedCells.Split(';');
                foreach (string cell in cells)
                {
                    string[] coordinates = cell.Split(',');
                    if (coordinates.Length != 2 ||
                        !int.TryParse(coordinates[0], out int x) ||
                        !int.TryParse(coordinates[1], out int y))
                    {
                        Log.Warning("地图阻挡格格式无效: {0}", cell);
                        continue;
                    }

                    if (x < 0 || x >= mapData.Width || y < 0 || y >= mapData.Height)
                    {
                        Log.Warning("地图阻挡格超出范围: {0}", cell);
                        continue;
                    }

                    // 房屋和围栏只阻挡地面角色，飞行物与子弹仍可通过。
                    grids[x, y] = new GridInfo("Grass", false, true);
                }
            }

            _mapInfo = new MapInfo(
                grids,
                new fix2(mapData.CellSizeX, mapData.CellSizeY),
                new fix2(mapData.OriginX, mapData.OriginY),
                mapData.Isometric
            );
            _mapAStar = MapAStar.Create(_mapInfo);
        }

        public void AddPathRequest(int2 start, int2 end, int unitId)
        {
            _mapAStar?.AddPathRequest(start, end, unitId);
        }

        public List<int2> GetPath(int unitId)
        {
            return _mapAStar?.GetPath(unitId);
        }

        public void RemovePathRequest(int unitId)
        {
            _mapAStar?.RemovePathRequest(unitId);
        }

        public override void Clear()
        {
            _mapAStar?.Dispose();
            _mapAStar = null;
            _mapInfo = null;
            _heroSpawnPoints = null;
            base.Clear();
        }
    }
}
