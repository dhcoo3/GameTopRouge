using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Map
{
    public class MapInfo
    {
        ///<summary>
        ///地图的单元格信息，对应角色坐标为[x,z]
        ///</summary>
        public GridInfo[,] grid;

        ///<summary>
        ///每个单元格的宽度和高度
        ///单位：米
        ///</summary>
        public fix2 gridSize{get;}

        /// <summary>逻辑格 (0, 0) 中心点在世界 XY 平面中的坐标。</summary>
        internal fix2 worldOrigin { get; }

        /// <summary>格子采用 Unity IsometricZAsY 投影。</summary>
        internal bool isometric { get; }

        ///<summary>
        ///获得地图的边界，单位：米
        ///</summary>
        public Rect border{get;}


        public MapInfo(GridInfo[,] map, fix2 gridSize) : this(map, gridSize, default, false)
        {
        }

        internal MapInfo(GridInfo[,] map, fix2 gridSize, fix2 worldOrigin, bool isometric){
            this.grid = map;
            this.gridSize = new fix2(
                fixMath.max(0.1f, gridSize.x),    //最小0.1米
                fixMath.max(0.1f, gridSize.y)
            );
            this.worldOrigin = worldOrigin;
            this.isometric = isometric;

            fix2 p0 = GridToWorld(-fix.Half, -fix.Half);
            fix2 p1 = GridToWorld(MapWidth() - fix.Half, -fix.Half);
            fix2 p2 = GridToWorld(-fix.Half, MapHeight() - fix.Half);
            fix2 p3 = GridToWorld(MapWidth() - fix.Half, MapHeight() - fix.Half);
            float minX = Mathf.Min(p0.x, p1.x, p2.x, p3.x);
            float maxX = Mathf.Max(p0.x, p1.x, p2.x, p3.x);
            float minY = Mathf.Min(p0.y, p1.y, p2.y, p3.y);
            float maxY = Mathf.Max(p0.y, p1.y, p2.y, p3.y);
            this.border = new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        ///<summary>
        ///地图的宽度
        ///<return>单位：单元格</return>
        ///</summary>
        public int MapWidth(){
            return grid.GetLength(0);
        }

        ///<summary>
        ///地图的高度
        ///<return>单位：单元格</return>
        ///</summary>
        public int MapHeight(){
            return grid.GetLength(1);
        }

        ///<summary>
        ///获得某个东西坐落在的位置信息
        ///<param name="pos">要检查的点，单位：米</param>
        ///<return>返回这个点的单元格信息</return>
        ///</summary>
        public GridInfo GetGridInPosition(fix3 pos){
            int2 gPos = GetGridPosByMeter(pos.x, pos.y);
            if (gPos.x < 0 || gPos.x >= MapWidth() || gPos.y < 0 || gPos.y >= MapHeight()) 
                return GridInfo.VoidGrid;
            return grid[gPos.x, gPos.y];
        }

        ///<summary>
        ///从地图上x坐标的米，获得单元格坐标x
        ///<param name="x">角色坐标x，单位：米</param>
        ///<param name="z">角色坐标z，单位：米</param>
        ///<return>单元格坐标，单位：单元格</return>
        ///</summary>
        public int2 GetGridPosByMeter(fix x, fix y){
            fix2 gridPos = GetGridCoordinateByMeter(x, y);
            return new int2(
                fixMath.roundToInt(gridPos.x),
                fixMath.roundToInt(gridPos.y)
            );
        }

        /// <summary>将地图格坐标转换为角色使用的世界 XY 坐标。</summary>
        internal fix3 GetWorldPosByGrid(int2 gridPos)
        {
            fix2 worldPos = GridToWorld(gridPos.x, gridPos.y);
            return new fix3(worldPos.x, worldPos.y, 0);
        }

        private fix2 GetGridCoordinateByMeter(fix x, fix y)
        {
            fix dx = x - worldOrigin.x;
            fix dy = y - worldOrigin.y;
            if (isometric)
            {
                // IsometricZAsY: worldX = (gridX-gridY)*cellX/2,
                //                worldY = (gridX+gridY)*cellY/2.
                fix isoX = dx / gridSize.x;
                fix isoY = dy / gridSize.y;
                return new fix2(isoX + isoY, isoY - isoX);
            }

            return new fix2(dx / gridSize.x, dy / gridSize.y);
        }

        private fix2 GridToWorld(fix gridX, fix gridY)
        {
            if (isometric)
            {
                return new fix2(
                    worldOrigin.x + (gridX - gridY) * gridSize.x / 2,
                    worldOrigin.y + (gridX + gridY) * gridSize.y / 2
                );
            }

            return new fix2(
                worldOrigin.x + gridX * gridSize.x,
                worldOrigin.y + gridY * gridSize.y
            );
        }

        ///<summary>
        ///判断某种移动模式下，某个单元格是否可过
        ///<param name="gridX">单元格坐标x</param>
        ///<param name="gridY">单元格坐标y</param>
        ///<param name="moveType">移动方式</param>
        ///<param name="ignoreBorder">是否把地图外的区域都当做可过</param>
        ///<return>是否可过</return>
        ///</summary>
        public bool CanGridPasses(int gridX, int gridY, MoveType moveType, bool ignoreBorder){
            if (gridX < 0 || gridX >= MapWidth() || gridY < 0 || gridY >= MapHeight()) return ignoreBorder;
            switch (moveType){
                case MoveType.ground: return grid[gridX, gridY].groundCanPass;
                case MoveType.fly: return grid[gridX, gridY].flyCanPass;
            }
            return false;
        }

        ///<summary>
        ///判断一个单位是否可以站在某个位置上
        ///<param name="pos">单位的位置，单位：米</param>
        ///<param name="radius">单位的半径，单位：米</param>
        ///<param name="moveType">单位移动模式</param>
        ///<return>是否可以站在这里，true代表可以</return>
        ///</summary>
        public bool CanUnitPlacedHere(fix3 pos, fix radius, MoveType moveType){
            if (isometric)
            {
                return CanIsometricUnitPlacedHere(pos, radius, moveType, false);
            }

            int2 lt = GetGridPosByMeter(pos.x - radius, pos.y - radius);
            int2 rb = GetGridPosByMeter(pos.x + radius, pos.y + radius);
            int aw = rb.x - lt.x + 1;
            int ah = rb.y - lt.y + 1;
            List<Rect> collisionRects = new List<Rect>();
            for (int i = lt.x; i <= rb.x; i++){
                for (int j = lt.y; j <= rb.y; j++){
                    if (CanGridPasses(i, j, moveType, false) == false){
                        collisionRects.Add(new Rect(
                            worldOrigin.x + (i - 0.5f) * gridSize.x,
                            worldOrigin.y + (j - 0.5f) * gridSize.y,
                            gridSize.x,
                            gridSize.y
                        ));
                    }
                    
                }
            }
            return !Utils.CircleHitRects(new fix2(pos.x, pos.y), radius, collisionRects);
            // Vector2Int gPos = GetGridPosByMeter(pos.x, pos.z);
            // if (gPos.x < 0 || gPos.y < 0 || gPos.x >= MapWidth() || gPos.y >= MapHeight()) return false;
            // return grid[gPos.x, gPos.y].groundCanPass;
        }

        private bool CanIsometricUnitPlacedHere(fix3 pos, fix radius, MoveType moveType, bool ignoreBorder)
        {
            fix2 gridPos = GetGridCoordinateByMeter(pos.x, pos.y);
            // 逆等距变换会把世界圆形拉成椭圆。以其轴向最大伸展量扩张格子，
            // 作为候选格范围；最终用圆形与菱形格子的实际边缘距离判断，避免整格膨胀。
            fix inverseScale = fixMath.sqrt(
                fix.One / (gridSize.x * gridSize.x) +
                fix.One / (gridSize.y * gridSize.y)
            );
            fix padding = radius * inverseScale;
            fix halfExtent = fix.Half + padding;
            int minX = fixMath.floorToInt(gridPos.x - halfExtent);
            int maxX = fixMath.ceilToInt(gridPos.x + halfExtent);
            int minY = fixMath.floorToInt(gridPos.y - halfExtent);
            int maxY = fixMath.ceilToInt(gridPos.y + halfExtent);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    if (CanGridPasses(x, y, moveType, ignoreBorder)) continue;
                    if (CircleIntersectsIsometricCell(new fix2(pos.x, pos.y), gridPos, x, y, radius))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool CircleIntersectsIsometricCell(fix2 point, fix2 gridPos, int gridX, int gridY, fix radius)
        {
            // 逻辑格在投影前是边长 1 的正方形；投影后是地图上的菱形。
            if (fixMath.abs(gridPos.x - gridX) <= fix.Half &&
                fixMath.abs(gridPos.y - gridY) <= fix.Half)
            {
                return true;
            }

            fix2 p0 = GridToWorld(gridX - fix.Half, gridY - fix.Half);
            fix2 p1 = GridToWorld(gridX + fix.Half, gridY - fix.Half);
            fix2 p2 = GridToWorld(gridX + fix.Half, gridY + fix.Half);
            fix2 p3 = GridToWorld(gridX - fix.Half, gridY + fix.Half);
            fix radiusSquared = radius * radius;
            return CircleIntersectsSegment(point, p0, p1, radiusSquared) ||
                   CircleIntersectsSegment(point, p1, p2, radiusSquared) ||
                   CircleIntersectsSegment(point, p2, p3, radiusSquared) ||
                   CircleIntersectsSegment(point, p3, p0, radiusSquared);
        }

        private static bool CircleIntersectsSegment(fix2 point, fix2 start, fix2 end, fix radiusSquared)
        {
            fix2 edge = end - start;
            fix edgeLengthSquared = edge.lengthSquared;
            if (edgeLengthSquared <= fix.Zero) return (point - start).lengthSquared <= radiusSquared;

            fix t = fixMath.clamp(fix2.Dot(point - start, edge) / edgeLengthSquared, fix.Zero, fix.One);
            fix2 closestPoint = start + edge * t;
            return (point - closestPoint).lengthSquared <= radiusSquared;
        }

            
        ///<summary>
        ///随机获得一个坐标，可以让角色站立在那里
        ///获得的坐标单位是米的，而非单元格，其坐标y始终为0
        ///如果实在没有这个位置，就会返回vector3.zero
        ///<param name="range">一个随机的范围，单位：单元格</param>
        ///<param name="chaRadius">角色半径，单位：米</param>
        ///<param name="moveType">单位移动模式</param>
        ///<return>可以落脚的坐标点</return>
        public fix3 GetRandomPosForCharacter(RectInt range, fix chaRadius, MoveType moveType = MoveType.ground){
            List<fix3> mayRes = new List<fix3>();
            for (var i = range.x; i < range.x + range.width; i ++){
                for (var j = range.y; j < range.y + range.height; j++){
                    //if (i >= 0 && i < MapWidth() && j >= 0 && j < MapHeight() && gridInfo[i, j].characterCanPass == true){
                    fix3 ranPos = new fix3(
                        GridToWorld(i, j).x,
                        GridToWorld(i, j).y,
                        0
                    );
                    if (CanUnitPlacedHere(ranPos, chaRadius, moveType) == true) {  
                        mayRes.Add(ranPos);
                    }
                }
            }
            if (mayRes.Count == 0) return fix3.zero;
            return mayRes[fixMath.floorToInt(GamePlayFacade.Instance.Random.NextInt(0, mayRes.Count))];
        }
        
        ///<summary>
        ///从一个点（单位米）出发获得方向上的第一个竖着的阻挡
        ///<param name="pivot">出发的点，单位：米</param>
        ///<param name="dir">查询方向以及长度，单位：米</param>
        ///<param name="radius">假设有一个半径（当做点是圆形中心），也就是额外追加一个距离，单位：米</param>
        ///<param name="moveType">移动方式</param>
        ///<param name="ignoreBorder">是否把地图外的区域都当做可过</param>
        ///<return>最合适的x坐标</return>
        ///</summary>
        public fix GetNearestVerticalBlock(fix3 pivot, fix dir, fix radius, MoveType moveType, bool ignoreBorder){
            if (dir == 0) return pivot.x;
            int dv = dir > 0 ? 1 : -1;
            fix bestX = pivot.x + dir;
            int seekWidth = fixMath.ceilToInt((fixMath.abs(dir) + radius) / gridSize.x + 2);
            int2 gPos = GetGridPosByMeter(pivot.x, pivot.y);
            for (var i = 0; i < seekWidth; i++){
                int cgX = gPos.x + dv * i;
                if (this.CanGridPasses(cgX, gPos.y, moveType, ignoreBorder) == false){
                    fix wallX = worldOrigin.x + (cgX - dv * 0.5f) * gridSize.x - dv * radius;
                    if (dv > 0){
                        return fixMath.min(wallX, bestX);
                    }else{
                        return fixMath.max(wallX, bestX);
                    }
                }
            }
            return bestX;
        }

        ///<summary>
        ///从一个点（单位米）出发获得方向上的第一个横着的阻挡
        ///<param name="pivot">出发的点，单位：米</param>
        ///<param name="dir">查询方向以及高度，单位：米</param>
        ///<param name="radius">假设有一个半径（当做点是圆形中心），也就是额外追加一个距离，单位：米</param>
        ///<param name="moveType">移动方式</param>
        ///<param name="ignoreBorder">是否把地图外的区域都当做可过</param>
        ///<return>最合适的z坐标</return>
        ///</summary>
        public fix GetNearestHorizontalBlock(fix3 pivot, fix dir, fix radius, MoveType moveType, bool ignoreBorder){
            if (dir == 0) return pivot.y;
            int dv = dir > 0 ? 1 : -1;
            fix bestZ = pivot.y + dir;
            int seekHeight = fixMath.ceilToInt((fixMath.abs(dir) + radius) / gridSize.y + 2);
            int2 gPos = GetGridPosByMeter(pivot.x, pivot.y);
            for (var i = 0; i < seekHeight; i++){
                int cgY = gPos.y + dv * i;
                if (this.CanGridPasses(gPos.x, cgY, moveType, ignoreBorder) == false){
                    fix wallZ = worldOrigin.y + (cgY - dv * 0.5f) * gridSize.y - dv * radius;
                    if (dv > 0){
                        return fixMath.min(wallZ, bestZ);
                    }else{
                        return fixMath.max(wallZ, bestZ);
                    }
                }
            }
            return bestZ;
        }

        ///<summary>
        ///根据一个圆（中心点和半径），获得期望移动到某个坐标的最理想的点
        ///TODO 只有移动速度不那么快的时候才在大多数时候工作正常……
        ///<param name="pivot">这个圆形的中心点坐标</param>
        ///<param name="radius">这个圆形的半径</param>
        ///<param name="targetPos">这个圆形期望移动到的坐标</param>
        ///<param name="moveType">圆形的移动方式</param>
        ///<param name="ignoreBorder">是否把地图外的区域都当做可过</param>
        ///<return>应该移动到的坐标</return>
        ///</summary>
        public MapTargetPosInfo FixTargetPosition(fix3 pivot, fix radius, fix3 targetPos, MoveType moveType, bool ignoreBorder){
            if (isometric)
            {
                return FixIsometricTargetPosition(pivot, radius, targetPos, moveType, ignoreBorder);
            }

            fix xDir = targetPos.x - pivot.x;
            fix zDir = targetPos.y - pivot.y;
            fix bestX = GetNearestVerticalBlock(pivot, xDir, radius, moveType, ignoreBorder);
            fix bestZ = GetNearestHorizontalBlock(pivot, zDir, radius, moveType, ignoreBorder);

            bool obstacled =  (
                fixMath.roundToInt(bestX * 1000) != fixMath.roundToInt(targetPos.x * 1000) || 
                fixMath.roundToInt(bestZ * 1000) != fixMath.roundToInt(targetPos.y * 1000)
            );
            return new MapTargetPosInfo(obstacled, new fix3(bestX, bestZ, targetPos.z));
        }

        /// <summary>修正等距地图上的碰撞位置，并选择最接近期望位置的沿墙滑动方向。</summary>
        /// <param name="pivot">本帧移动前的角色中心，使用世界 XY 坐标。</param>
        /// <param name="radius">角色碰撞半径，单位为米。</param>
        /// <param name="targetPos">本帧期望到达的世界坐标。</param>
        /// <param name="moveType">用于判断地面或飞行通行性的移动方式。</param>
        /// <param name="ignoreBorder">是否允许移动到地图格范围之外。</param>
        /// <returns>碰撞标记及可到达的建议位置。</returns>
        private MapTargetPosInfo FixIsometricTargetPosition(fix3 pivot, fix radius, fix3 targetPos, MoveType moveType, bool ignoreBorder)
        {
            if (CanIsometricUnitPlacedHere(targetPos, radius, moveType, ignoreBorder))
            {
                return new MapTargetPosInfo(false, targetPos);
            }

            // 先沿本帧移动线二分到最近的可行位置，再尝试沿墙滑动。
            fix low = fix.Zero;
            fix high = fix.One;
            fix3 best = pivot;
            for (int i = 0; i < 12; i++)
            {
                fix mid = (low + high) / 2;
                fix3 candidate = new fix3(
                    pivot.x + (targetPos.x - pivot.x) * mid,
                    pivot.y + (targetPos.y - pivot.y) * mid,
                    targetPos.z
                );
                if (CanIsometricUnitPlacedHere(candidate, radius, moveType, ignoreBorder))
                {
                    low = mid;
                    best = candidate;
                }
                else
                {
                    high = mid;
                }
            }

            fix3 slideX = new fix3(targetPos.x, best.y, targetPos.z);
            fix3 slideY = new fix3(best.x, targetPos.y, targetPos.z);
            bool canSlideX = CanIsometricUnitPlacedHere(slideX, radius, moveType, ignoreBorder);
            bool canSlideY = CanIsometricUnitPlacedHere(slideY, radius, moveType, ignoreBorder);
            if (canSlideX || canSlideY)
            {
                // 通用定点幂通过对数计算，不接受负底数；距离平方直接相乘，兼容正负位移。
                fix2 offsetX = new fix2(targetPos.x - slideX.x, targetPos.y - slideX.y);
                fix2 offsetY = new fix2(targetPos.x - slideY.x, targetPos.y - slideY.y);
                fix distanceX = canSlideX ? offsetX.lengthSquared : fix.MaxValue;
                fix distanceY = canSlideY ? offsetY.lengthSquared : fix.MaxValue;
                best = distanceX <= distanceY ? slideX : slideY;
            }

            return new MapTargetPosInfo(true, best);
        }
    }
}
