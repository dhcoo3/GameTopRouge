using System.Collections.Generic;
using UnityEngine;

public class Utils{

    public static bool CircleHitRects(fix2 circlePivot, fix circleRadius, List<Rect> rects){
        if (rects.Count <= 0) return false;
        for (var i = 0; i < rects.Count; i++){
            if (Utils.CircleHitRect(circlePivot, circleRadius, rects[i]) == true){
                return true;
            }
        }
        return false;
    }
    public static bool CircleHitRects(fix2 circlePivot, fix circleRadius, Rect[] rects){
        List<Rect> rl = new List<Rect>();
        for (var i = 0; i < rects.Length; i++){
            rl.Add(rects[i]);
        }
        return CircleHitRects(circlePivot, circleRadius, rl);
    }

    public static bool CircleHitRect(fix2 circlePivot, fix circleRadius, Rect rect){
        int xp = circlePivot.x < rect.x ? 0 : (circlePivot.x > rect.x + rect.width ? 2 : 1);
        int yp = circlePivot.y < rect.y ? 0 : (circlePivot.y > rect.y + rect.height ? 2 : 1);
        
        if (yp == 1 && xp == 1) return true;  //在中间，则一定命中
        
        if (yp != 1 && xp == 1){
            fix halfRect = rect.height / 2;
            fix toHeart = fixMath.abs(circlePivot.y - (rect.y + halfRect));
            return (toHeart <= circleRadius + halfRect);
        }else
        if (yp == 1 && xp != 1){
            fix halfRect = rect.width / 2;
            fix toHeart = fixMath.abs(circlePivot.x - (rect.x + halfRect));
            return (toHeart <= circleRadius + halfRect);
        }else{
            return InRange(
                circlePivot.x, circlePivot.y, 
                yp == 0 ? rect.x : (rect.x + rect.width), 
                xp == 0 ? rect.y : (rect.y + rect.height), 
                circleRadius
            );
        }
    }

    ///<summary>
    ///AABB的矩形之间是否有碰撞
    ///<param name="a">一个rect</param>
    ///<param name="b">另一个rect</param>
    ///<return>是否碰撞到了，true代表碰到了</return>
    ///</summary>
    public static bool RectCollide(Rect a, Rect b){
        fix ar = a.x + a.width;
        fix br = b.x + b.width;
        fix ab = a.y + a.height;
        fix bb = b.y + b.height;
        return (
            (a.x >= b.x && a.x <= br) ||
            (b.x >= a.x && b.x <= ar)
        ) && (
            (a.y >= b.y && a.y <= bb) ||
            (b.y >= a.y && b.y <= ab)
        );
    }

   
    public static bool InRange(fix x1, fix y1, fix x2, fix y2, fix range){
        return fixMath.pow(x1 - x2, 2) + fixMath.pow(y1 - y2, 2) <= fixMath.pow(range,  2);
    }
    
    // 完全参数化版本（XY平面）
    public static bool IsTargetInSectorArea(fix3 selfPosition, fix selfRotationZ, fix3 targetPosition, 
        fix radius, fix degree)
    {
        if (degree == 0) return false;

        // 距离检查
        fix3 toTarget = targetPosition - selfPosition;
   
        if (fixMath.lengthsq(toTarget) > radius * radius)
            return false;

        // 角度检查
        fix3 characterForward = GetDirectionFromRotation(selfRotationZ);
        fix3 directionToTarget = toTarget.normalized;
        fix angle = SignedAngle(characterForward, directionToTarget, fix3.forward);
        fix halfAngle = fixMath.deg(degree / 2); // 将度数转换为弧度

        return fixMath.abs(angle) <= halfAngle;
    }

    // 2D版本（XY平面）
    public static bool IsTargetInSectorArea2D(fix2 selfPos, fix facingAngle, fix2 enemyPos, 
        fix sectorRadius, fix sectorAngle)
    {
        // 计算相对位置向量
        fix2 directionToEnemy = enemyPos - selfPos;
        
        // 检查距离是否在半径范围内
        fix distance = directionToEnemy.length;
        if (distance > sectorRadius)
            return false;
    
        // 计算敌人相对于正X轴的角度（弧度）
        fix enemyAbsoluteAngle = fixMath.atan2(directionToEnemy.y, directionToEnemy.x);
    
        // 将facingAngle转换为弧度并计算相对角度
        fix enemyRelativeAngle = enemyAbsoluteAngle - facingAngle;
    
        // 规范化角度到 [-π, π] 范围
        enemyRelativeAngle = NormalizeAngle(enemyRelativeAngle);
    
        // 检查角度是否在扇形区域内（将sectorAngle转换为弧度）
        fix halfSectorAngle = fixMath.deg(sectorAngle / 2);
        return fixMath.abs(enemyRelativeAngle) <= halfSectorAngle;
    }

    // 根据Z轴旋转获取朝向向量（XY平面）
    private static fix3 GetDirectionFromRotation(fix rotationZ)
    {
        return new fix3(fixMath.cos(rotationZ), fixMath.sin(rotationZ), 0); // XY平面
    }

    // 计算有符号角度（两个向量之间的角度，带方向）
    private static fix SignedAngle(fix3 from, fix3 to, fix3 axis)
    {
        fix3 crossProduct = fixMath.cross(from, to);
        fix unsignedAngle = fixMath.acos(fixMath.dot(from, to));
        fix sign = fixMath.sign(fixMath.dot(crossProduct, axis));
        return unsignedAngle * sign;
    }

    // 规范化角度到 [-π, π] 范围
    private static fix NormalizeAngle(fix angle)
    {
        angle = fixMath.WrapAngle(angle);
        return angle;
    }
    
    public static void FormatDirection(ref fix angle)
    {
        // Adjust the ranges based on your snapped angles
        if ((angle >= 330 || angle < 15))
            angle = 0; // Corresponds to angle 0
        else if ((angle >= 15 && angle < 60))
            angle = 35; // Corresponds to angle 35
        else if ((angle >= 60 && angle < 120))
            angle = 90; // Corresponds to angle 90
        else if ((angle >= 120 && angle < 165))
            angle = 150; // Corresponds to angle 150
        else if ((angle >= 165 && angle < 195))
            angle = 180; // Corresponds to angle 180
        else if ((angle >= 195 && angle < 240))
            angle = 215; // Corresponds to angle 215
        else if ((angle >= 240 && angle < 300))
            angle = 270; // Corresponds to angle 270
        else if ((angle >= 300 && angle < 345))
            angle = 330; // Corresponds to angle 330
    }

    /// <summary>
    /// 根据面得到一个八方向
    /// <param name="faceDegree">面向角度</param>
    /// </summary>
    public static cfg.Anim.Direction GetEightDirection(fix faceDegree)
    {
        if (faceDegree == 0)
        {
            return cfg.Anim.Direction.Right;
        }
        else if (faceDegree >= 15 && faceDegree < 60)
        {
            return cfg.Anim.Direction.RightTop;
        }
        else if (faceDegree >= 60 && faceDegree < 120)
        {
            return cfg.Anim.Direction.Top;
        }
        else if (faceDegree >= 120 && faceDegree < 165)
        {
            return cfg.Anim.Direction.LeftTop;
        }
        else if (faceDegree >= 165 && faceDegree < 195)
        {
            return cfg.Anim.Direction.Left;
        }
        else if (faceDegree >= 195 && faceDegree < 240)
        {
            return cfg.Anim.Direction.LeftBottom;
        }
        else if (faceDegree >= 240 && faceDegree < 300)
        {
            return cfg.Anim.Direction.Bottom;
        }
        else if (faceDegree >= 300 && faceDegree < 345)
        {
            return cfg.Anim.Direction.RightBottom;
        }
        
        return cfg.Anim.Direction.Null;
    }

    /// <summary>
    /// 将战斗相机的世界坐标，转换到UI坐标上
    /// </summary>
    /// <param name="worldPosition"></param>
    /// <returns></returns>
    public static Vector2 WorldToUIPoint(Vector3 worldPosition)
    {
        if (!GF.MainCamera)
        {
            return Vector2.zero;
        }
        
        // 1. 将3D世界坐标转换为战斗相机的视口坐标
        Vector3 viewportPoint = GF.MainCamera.WorldToViewportPoint(worldPosition);
        
        // 3. 将视口坐标转换为UI相机的屏幕坐标
        Vector2 screenPoint = new Vector2(
            viewportPoint.x * Screen.width,
            viewportPoint.y * Screen.height
        );
        
        // 4. 将屏幕坐标转换为UI Canvas的本地坐标
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            GF.UIRoot,
            screenPoint,
            GF.UICamera,
            out localPoint
        );
        return localPoint;
    }
}