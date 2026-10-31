using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Job
{
    public struct PathNode
    {
        public int Index; // 节点在网格中的扁平化索引
        public int2 Coordinate; // 节点在网格中的坐标（使用 Mathematics 的 int2）
        public int GCost; // 从起点到该节点的实际代价
        public int HCost; // 该节点到终点的启发式估算代价
        public int FCost; // 总代价 (gCost + hCost)
        public int CameFromNodeIndex; // 当前节点的父节点索引，用于回溯路径
        public bool IsWalkable; // 该节点是否可行走

        // 计算 FCost
        public void CalculateFCost()
        {
            FCost = GCost + HCost;
        }
    }
    
    // MinHeap 结构体（需要在同一个文件中定义）
    public struct MinHeap
    {
        public NativeList<int> Heap;
        [ReadOnly] public NativeArray<PathNode> Nodes;

        public MinHeap(int capacity, NativeArray<PathNode> nodeArray, Allocator allocator)
        {
            Heap = new NativeList<int>(capacity, allocator);
            Nodes = nodeArray;
        }

        public void Push(int item)
        {
            Heap.Add(item);
            BubbleUp(Heap.Length - 1);
        }

        public int Pop()
        {
            int result = Heap[0];
            int lastIndex = Heap.Length - 1;
            Heap[0] = Heap[lastIndex];
            Heap.RemoveAt(lastIndex);

            if (Heap.Length > 0)
                SinkDown(0);

            return result;
        }

        public bool Contains(int item)
        {
            for (int i = 0; i < Heap.Length; i++)
            {
                if (Heap[i] == item)
                    return true;
            }

            return false;
        }

        public bool IsEmpty => Heap.Length == 0;
        public int Length => Heap.Length;

        private void BubbleUp(int index)
        {
            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;
                if (CompareNodes(Heap[index], Heap[parentIndex]) >= 0)
                    break;

                Swap(index, parentIndex);
                index = parentIndex;
            }
        }

        private void SinkDown(int index)
        {
            int length = Heap.Length;
            while (true)
            {
                int leftChild = 2 * index + 1;
                int rightChild = 2 * index + 2;
                int smallest = index;

                if (leftChild < length && CompareNodes(Heap[leftChild], Heap[smallest]) < 0)
                    smallest = leftChild;

                if (rightChild < length && CompareNodes(Heap[rightChild], Heap[smallest]) < 0)
                    smallest = rightChild;

                if (smallest == index)
                    break;

                Swap(index, smallest);
                index = smallest;
            }
        }

        private int CompareNodes(int indexA, int indexB)
        {
            PathNode nodeA = Nodes[indexA];
            PathNode nodeB = Nodes[indexB];

            // 首先比较 F 成本
            if (nodeA.FCost < nodeB.FCost) return -1;
            if (nodeA.FCost > nodeB.FCost) return 1;

            // F 成本相同时，比较 H 成本（倾向于选择离目标更近的节点）
            if (nodeA.HCost < nodeB.HCost) return -1;
            if (nodeA.HCost > nodeB.HCost) return 1;

            return 0;
        }

        private void Swap(int a, int b)
        {
            int temp = Heap[a];
            Heap[a] = Heap[b];
            Heap[b] = temp;
        }

        public void Dispose()
        {
            if (Heap.IsCreated)
                Heap.Dispose();
        }

        public bool IsCreated()
        {
            return Heap.IsCreated;
        }
    }
    
    // 批量寻路请求
    public struct BatchPathRequest
    {
        public int2 StartPosition;
        public int2 EndPosition;
        public int UnitId;
    }

    // 批量寻路结果
    public struct BatchPathResult
    {
        public bool PathFound;
        public int UnitId;
        public int Length;
    }

    [BurstCompile]
    public struct AStarParallelForJob : IJobParallelFor
    {
        // 共享的只读数据
        [ReadOnly] public int2 GridSize;
        [ReadOnly] public int MaxPathCount;
        [ReadOnly] public NativeArray<bool> WalkableMap;
        
        // 批量输入：所有寻路请求
        [ReadOnly] public NativeArray<BatchPathRequest> PathRequests;
        
        // 批量输出：所有寻路结果
        [WriteOnly] public NativeArray<BatchPathResult> PathResults;
        
        [NativeDisableParallelForRestriction]
        public NativeArray<int2> PathPoints;

        public void Execute(int index)
        {
            var request = PathRequests[index];
            
            var result = new BatchPathResult
            {
                UnitId = request.UnitId,
                PathFound = false
            };
            
            NativeList<int2> path = new NativeList<int2>(MaxPathCount, Allocator.Temp);
            
            // 执行单个单位的 A* 寻路
            if (FindPath(request.StartPosition, request.EndPosition, ref path))
            {
                int baseIndex = index * MaxPathCount;
                
                for (int i = 0; i < math.min(path.Length, MaxPathCount); i++)
                {
                    PathPoints[baseIndex + i] = path[i];
                }
                
                result.PathFound = true;
                result.Length = path.Length;
            }
            
            PathResults[index] = result;
        }
        
        private bool FindPath(int2 start, int2 end, ref NativeList<int2> outputPath)
        {
            int totalNodes = GridSize.x * GridSize.y;
            NativeArray<PathNode> nodes = new NativeArray<PathNode>(totalNodes, Allocator.Temp);
            MinHeap openList = new MinHeap(100, nodes, Allocator.Temp);
            NativeArray<bool> closedList = new NativeArray<bool>(totalNodes, Allocator.Temp);
            
            // 初始化节点网格
            InitializeNodes(nodes);
            
            int startIndex = CalculateIndex(start, GridSize.x);
            int endIndex = CalculateIndex(end, GridSize.x);
            
            // 设置起点
            PathNode startNode = nodes[startIndex];
            startNode.GCost = 0;
            startNode.HCost = CalculateHeuristic(start, end);
            startNode.CalculateFCost();
            nodes[startIndex] = startNode;
            openList.Push(startIndex);
            
            // A* 主循环
            while (!openList.IsEmpty)
            {
                int currentNodeIndex = openList.Pop();
                
                if (currentNodeIndex == endIndex)
                {
                    RetracePath(startIndex, endIndex, nodes, ref outputPath);
                    
                    // 清理资源
                    if (openList.IsCreated())
                        openList.Dispose();
                    if (closedList.IsCreated)
                        closedList.Dispose();
                    if (nodes.IsCreated)
                        nodes.Dispose();
                    return true;
                }
                
                closedList[currentNodeIndex] = true;
               
                NativeList<int> neighbours = GetNeighbours(currentNodeIndex, GridSize, nodes);
                
                for (int i = 0; i < neighbours.Length; i++)
                {
                    int neighbourIndex = neighbours[i];
                    
                    if (closedList[neighbourIndex] || !nodes[neighbourIndex].IsWalkable)
                        continue;

                    // Do not cut diagonally through the corner of a blocked cell.
                    int2 currentCoordinate = nodes[currentNodeIndex].Coordinate;
                    int2 neighbourCoordinate = nodes[neighbourIndex].Coordinate;
                    int dx = neighbourCoordinate.x - currentCoordinate.x;
                    int dy = neighbourCoordinate.y - currentCoordinate.y;
                    if (dx != 0 && dy != 0)
                    {
                        int horizontalIndex = CalculateIndex(new int2(currentCoordinate.x + dx, currentCoordinate.y), GridSize.x);
                        int verticalIndex = CalculateIndex(new int2(currentCoordinate.x, currentCoordinate.y + dy), GridSize.x);
                        if (!nodes[horizontalIndex].IsWalkable || !nodes[verticalIndex].IsWalkable)
                            continue;
                    }
                    
                    PathNode neighbourNode = nodes[neighbourIndex];
                    int newGCost = nodes[currentNodeIndex].GCost + (dx != 0 && dy != 0 ? 14 : 10);
                    
                    if (newGCost < neighbourNode.GCost)
                    {
                        neighbourNode.CameFromNodeIndex = currentNodeIndex;
                        neighbourNode.GCost = newGCost;
                        neighbourNode.HCost = CalculateHeuristic(
                            IndexToCoordinate(neighbourIndex, GridSize.x), end);
                        neighbourNode.CalculateFCost();
                        nodes[neighbourIndex] = neighbourNode;
                        
                        if (!openList.Contains(neighbourIndex))
                        {
                            openList.Push(neighbourIndex);
                        }
                    }
                }
                
                neighbours.Dispose();
            }
            
            // 清理资源
            if (openList.IsCreated())
                openList.Dispose();
            if (closedList.IsCreated)
                closedList.Dispose();
            if (nodes.IsCreated)
                nodes.Dispose();
            
            return false;
        }
        
        private void InitializeNodes(NativeArray<PathNode> nodes)
        {
            for (int y = 0; y < GridSize.y; y++)
            {
                for (int x = 0; x < GridSize.x; x++)
                {
                    int index = CalculateIndex(new int2(x, y), GridSize.x);
                    nodes[index] = new PathNode
                    {
                        Index = index,
                        Coordinate = new int2(x, y),
                        GCost = int.MaxValue,
                        HCost = 0,
                        FCost = int.MaxValue,
                        CameFromNodeIndex = -1,
                        IsWalkable = WalkableMap[index]
                    };
                }
            }
        }
        
        private void RetracePath(int startNodeIndex, int endNodeIndex, NativeArray<PathNode> nodes, ref NativeList<int2> outputPath)
        {
            // 使用一个固定大小的临时数组来存储路径（假设路径不会超过一定长度，比如1000）
            NativeArray<int2> tempPath = new NativeArray<int2>(1000, Allocator.Temp);
            int count = 0;
            int currentNodeIndex = endNodeIndex;

            // 从终点回溯到起点，并存储坐标
            while (currentNodeIndex != startNodeIndex && count < tempPath.Length)
            {
                tempPath[count] = nodes[currentNodeIndex].Coordinate;
                count++;
                currentNodeIndex = nodes[currentNodeIndex].CameFromNodeIndex;
                if (currentNodeIndex == -1)
                {
                    break;
                }
            }

            // 将临时数组的内容按逆序（即从起点到终点的正确顺序）复制到 outputPath
            outputPath.Clear();
            for (int i = count - 1; i >= 0; i--)
            {
                outputPath.Add(tempPath[i]);
            }

            tempPath.Dispose();
        }
        
        // 其他辅助方法保持不变...
        private int CalculateIndex(int2 coordinate, int gridWidth)
        {
            return coordinate.y * gridWidth + coordinate.x;
        }

        private int2 IndexToCoordinate(int index, int gridWidth)
        {
            return new int2(index % gridWidth, index / gridWidth);
        }

        private NativeList<int> GetNeighbours(int currentNodeIndex, int2 gridSize, NativeArray<PathNode> nodes)
        {
            NativeList<int> neighbours = new NativeList<int>(8, Allocator.Temp);
            int2 currentCoord = IndexToCoordinate(currentNodeIndex, gridSize.x);

            // 八方向移动
            NativeArray<int2> directions = new NativeArray<int2>(8, Allocator.Temp);
            directions[0] = new int2(0, 1);
            directions[1] = new int2(1, 1);
            directions[2] = new int2(1, 0);
            directions[3] = new int2(1, -1);
            directions[4] = new int2(0, -1);
            directions[5] = new int2(-1, -1);
            directions[6] = new int2(-1, 0);
            directions[7] = new int2(-1, 1);

            foreach (var dir in directions)
            {
                int2 neighbourCoord = currentCoord + dir;

                if (neighbourCoord.x >= 0 && neighbourCoord.x < gridSize.x &&
                    neighbourCoord.y >= 0 && neighbourCoord.y < gridSize.y)
                {
                    int neighbourIndex = CalculateIndex(neighbourCoord, gridSize.x);
                    neighbours.Add(neighbourIndex);
                }
            }

            directions.Dispose();

            return neighbours;
        }
        
        /*
        // 四方向邻居获取方法
        private NativeList<int> GetNeighbours4Direction(int currentNodeIndex, int2 gridSize, NativeArray<PathNode> nodes)
        {
            NativeList<int> neighbours = new NativeList<int>(4, Allocator.Temp);
            int2 currentCoord = IndexToCoordinate(currentNodeIndex, gridSize.x);
        
            // 四个基本方向：上、右、下、左
            int2[] directions = new int2[]
            {
                new int2(0, 1),   // 上
                new int2(1, 0),   // 右
                new int2(0, -1),  // 下
                new int2(-1, 0)   // 左
            };
        
            foreach (var dir in directions)
            {
                int2 neighbourCoord = currentCoord + dir;
            
                // 检查坐标是否在网格范围内
                if (neighbourCoord.x >= 0 && neighbourCoord.x < gridSize.x &&
                    neighbourCoord.y >= 0 && neighbourCoord.y < gridSize.y)
                {
                    int neighbourIndex = CalculateIndex(neighbourCoord, gridSize.x);
                    neighbours.Add(neighbourIndex);
                }
            }
        
            return neighbours;
        }

        private int CalculateDistanceCost(int2 a, int2 b)
        {
            int dx = math.abs(a.x - b.x);
            int dy = math.abs(a.y - b.y);

            // 直线移动成本10，对角线移动成本14
            int straight = math.min(dx, dy);
            int diagonal = math.abs(dx - dy);
            return 14 * straight + 10 * diagonal;
        }
        
        
        // 四方向距离成本计算（每个移动固定成本10）
        private int CalculateDistance4Cost(int2 a, int2 b)
        {
            // 四方向移动，每个格子移动成本固定为10
            return 10;
        }*/

        private int CalculateHeuristic(int2 a, int2 b)
        {
            int dx = math.abs(a.x - b.x);
            int dy = math.abs(a.y - b.y);

            // 对角线距离启发式函数
            return 10 * (dx + dy) - 6 * math.min(dx, dy);
        }
    }
}
