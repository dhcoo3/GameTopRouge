using System.Collections.Generic;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Job;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Map
{
    public class MapAStar
    {
        // Hero and monster base body radii are both 0.25m. Use the same clearance
        // when baking the isometric navigation grid so path nodes are physically reachable.
        private static readonly fix IsometricPathBodyRadius = 0.25f;

        private MapInfo _mapInfo;
        private int Width => _mapInfo.MapWidth();
        private int Height => _mapInfo.MapHeight();
    
        public const int MAX_PATH_LENGTH = 100;
        
        private int _batchSize = 36;
    
        // 使用双缓冲队列来处理并发访问
        private readonly List<BatchPathRequest> _newRequests = new List<BatchPathRequest>(100);
        private readonly List<BatchPathRequest> _processingRequests = new List<BatchPathRequest>(100);
        
        private NativeArray<bool> _walkableMap;
        private NativeList<BatchPathRequest> _pendingRequests;
        private NativeArray<BatchPathResult> _pathResults;
        private NativeArray<int2> _pathPoints;
        private JobHandle _pathfindingJobHandle;

        private Dictionary<int, List<int2>> _pathRequestFinish = new Dictionary<int, List<int2>>();
        private bool _jobScheduled = false;
        private bool _jobCompleted = true;

        public static MapAStar Create(MapInfo mapInfo)
        {
            MapAStar mapAStar = new MapAStar();
            mapAStar._mapInfo = mapInfo;
            
            int2 gridSize = new int2(mapAStar.Width, mapAStar.Height);
            mapAStar.InitializeWalkableMap(gridSize);
            mapAStar._pendingRequests = new NativeList<BatchPathRequest>(Allocator.Persistent);
            
            return mapAStar;
        }
        
        private void InitializeWalkableMap(int2 gridSize)
        {
            _walkableMap = new NativeArray<bool>(gridSize.x * gridSize.y, Allocator.Persistent);
            
            for (int y = 0; y < gridSize.y; y++)
            {
                for (int x = 0; x < gridSize.x; x++)
                {
                    bool walkable = _mapInfo.grid[x, y].groundCanPass;
                    if (walkable && _mapInfo.isometric)
                    {
                        fix3 cellCenter = _mapInfo.GetWorldPosByGrid(new int2(x, y));
                        walkable = _mapInfo.CanUnitPlacedHere(cellCenter, IsometricPathBodyRadius, MoveType.ground);
                    }

                    _walkableMap[CalculateIndex(new int2(x, y), gridSize.x)] = walkable;
                }
            }
        }
        
        private int CalculateIndex(int2 coordinate, int gridWidth)
        {
            return coordinate.y * gridWidth + coordinate.x;
        }

        public bool HasPathRequest(int unitId)
        {
            return _pathRequestFinish.ContainsKey(unitId);
        }
        
        public void AddPathRequest(int2 start, int2 end, int unitId)
        {
            if (HasPathRequest(unitId)) return;
            
            lock (_newRequests) // 线程安全添加
            {
                _newRequests.Add(new BatchPathRequest
                {
                    StartPosition = start,
                    EndPosition = end,
                    UnitId = unitId
                });
            }
        }

        public void LogicUpdate(fix deltaTime)
        {
            // 1. 首先完成之前的Job并处理结果
            CompleteJobsAndProcessResults();

            // 2. 然后处理新请求
            ProcessNewRequests();

            // 3. 如果有待处理请求且当前没有Job在运行，开始新的批处理
            if (_pendingRequests.Length > 0 && !_jobScheduled && _jobCompleted)
            {
                ExecuteBatchPathfinding();
            }
        }
        
        private void ProcessNewRequests()
        {
            lock (_newRequests)
            {
                if (_newRequests.Count == 0) return;

                // 确保Job完成后再修改数据
                if (_jobScheduled)
                {
                    _pathfindingJobHandle.Complete();
                    _jobScheduled = false;
                    _jobCompleted = true;
                }

                // 将新请求添加到处理列表
                foreach (var request in _newRequests)
                {
                    if (!_pathRequestFinish.ContainsKey(request.UnitId))
                    {
                        _pathRequestFinish.Add(request.UnitId, new List<int2>());
                        _processingRequests.Add(request);
                    }
                }
                _newRequests.Clear();

                // 将处理列表中的请求转移到NativeList
                if (_processingRequests.Count > 0)
                {
                    // 确保_pendingRequests是空的或者已完成使用
                    if (_pendingRequests.IsCreated)
                    {
                        _pendingRequests.Clear();
                    }

                    foreach (var request in _processingRequests)
                    {
                        _pendingRequests.Add(request);
                    }
                    _processingRequests.Clear();
                }
            }
        }

        public void ExecuteBatchPathfinding()
        {
            if (_pendingRequests.Length == 0)
                return;
        
            // 确保之前的Job已完成
            if (_jobScheduled)
            {
                _pathfindingJobHandle.Complete();
                _jobScheduled = false;
            }
        
            // 清理之前的结果
            if (_pathResults.IsCreated)
                _pathResults.Dispose();

            if (_pathPoints.IsCreated)
                _pathPoints.Dispose();
        
            // 准备新的结果数组
            _pathResults = new NativeArray<BatchPathResult>(_pendingRequests.Length, Allocator.Persistent);
            _pathPoints = new NativeArray<int2>(_pendingRequests.Length * MAX_PATH_LENGTH, Allocator.Persistent);
            
            // 创建Job - 这里使用_pendingRequests.AsArray()的副本
            var pathRequestsCopy = new NativeArray<BatchPathRequest>(_pendingRequests.AsArray(), Allocator.TempJob);
            
            var parallelJob = new AStarParallelForJob
            {
                GridSize = new int2(Width, Height),
                WalkableMap = _walkableMap,
                PathRequests = pathRequestsCopy, // 使用副本而不是原始引用
                PathResults = _pathResults,
                PathPoints = _pathPoints,
                MaxPathCount = MAX_PATH_LENGTH
            };
        
            // 调度Job
            _pathfindingJobHandle = parallelJob.Schedule(_pendingRequests.Length, Mathf.Min(_pendingRequests.Length, _batchSize));
            
            // 添加依赖来释放临时数组
            _pathfindingJobHandle = pathRequestsCopy.Dispose(_pathfindingJobHandle);
            
            _jobScheduled = true;
            _jobCompleted = false;
        }

        private void CompleteJobsAndProcessResults()
        {
            if (_jobScheduled && _pathfindingJobHandle.IsCompleted)
            {
                _pathfindingJobHandle.Complete();
                _jobScheduled = false;
                _jobCompleted = true;
            
                if (_pathResults.IsCreated && _pathPoints.IsCreated)
                {
                    // 处理所有寻路结果
                    for (int i = 0; i < _pathResults.Length; i++)
                    {
                        var result = _pathResults[i];
                        
                        if (result.PathFound && result.Length > 0)
                        {
                            DistributePathToUnit(result.UnitId, i, result.Length);
                        }
                        else
                        {
                            //Debug.LogWarning($"Unit {result.UnitId}: No path found!");
                            if (_pathRequestFinish.ContainsKey(result.UnitId))
                            {
                                _pathRequestFinish.Remove(result.UnitId);
                            }
                        }
                    }
                
                    // 清理资源
                    _pathResults.Dispose();
                    _pathPoints.Dispose();
                    
                    // 清空已处理的请求
                    _pendingRequests.Clear();
                }
            }
        }
    
        private void DistributePathToUnit(int unitId, int requestIndex, int pathLength)
        {
            if (_pathRequestFinish.TryGetValue(unitId, out var list))
            {
                list.Clear();
                
                int baseIndex = requestIndex * MAX_PATH_LENGTH;
                
                for (int j = 0; j < pathLength; j++)
                {
                    int pointIndex = baseIndex + j;
                    if (pointIndex < _pathPoints.Length)
                    {
                        list.Add(_pathPoints[pointIndex]);
                    }
                }
                
                // 调试信息
                /*if (list.Count > 0)
                {
                    string pathStr = string.Join(" -> ", list);
                    Log.Info($"单位 {unitId} 寻路完成，路径点数量: {pathLength}, 路径: {pathStr}");
                }*/
            }
        }

        public List<int2> GetPath(int unitId)
        {
            if (_pathRequestFinish.TryGetValue(unitId, out var path))
            {
                return path.Count > 0 ? path : null;
            }
            return null;
        }
        
        public void RemovePathRequest(int unitId)
        {
            if (_pathRequestFinish.ContainsKey(unitId))
            {
                _pathRequestFinish.Remove(unitId);
            }
        }
        
        public void Dispose()
        {
            // 等待所有Job完成
            if (_jobScheduled)
            {
                _pathfindingJobHandle.Complete();
                _jobScheduled = false;
                _jobCompleted = true;
            }
            
            // 释放所有Native资源
            if (_walkableMap.IsCreated) _walkableMap.Dispose();
            if (_pendingRequests.IsCreated) _pendingRequests.Dispose();
            if (_pathResults.IsCreated) _pathResults.Dispose();
            if (_pathPoints.IsCreated) _pathPoints.Dispose();
            
            // 清理托管资源
            _newRequests.Clear();
            _processingRequests.Clear();
            _pathRequestFinish.Clear();
        }
        
        void OnDestroy()
        {
            Dispose();
        }
    }
}
