using System.Collections.Generic;
using UnityEngine;

namespace AAAGame.ScriptsHotfix.Common
{
    public static class MathUtils
    {
        public static fix PrecisionFix = new fix(1000);
        public static readonly int PrecisionInt = 1000;
        public static readonly fix OneHundred = 100;
        public static readonly fix Half = fix.Half;
        public static readonly fix Two = (fix)2;
        public static readonly fix Three = (fix)3;
        public static readonly fix Four = (fix)4;
        public static readonly fix ZeroOne = new fix(1) / new fix(10);
        public static readonly fix ZeroZeroOne = new fix(1) / new fix(100);
        public static readonly fix ZeroZeroZeroOne = new fix(1) / new fix(1000);
        public static readonly fix ZeroTwo = new fix(2) / new fix(10);
        public static readonly fix ZeroOneFive = new fix(15) / new fix(100);
        public static readonly fix ZeroFour = new fix(4) / new fix(10);
        public static fixQuaternion Angle90 = fixQuaternion.FromEuler(fixMath.radians(90), fix.Zero, fix.Zero);
        public static fixQuaternion AngleN90 = fixQuaternion.FromEuler(-fixMath.radians(90), fix.Zero, fix.Zero);

       public static fix3 Convert(Vector3Int source)
        {
            fix3 value = new fix3(source.x / PrecisionFix, source.y / PrecisionFix, source.z / PrecisionFix);
            return value;
        }
        
        public static fix Convert(int value)
        {
            return value / PrecisionFix;
        }

        public static fix2 Fix3ToFix2(fix3 value)
        {
            return new fix2(value.x, value.y);
        }

        public static void Convert(Vector3Int[] source, List<fix3> target)
        {
            foreach (var t in source)
            {
                fix3 val = Convert(t);
                target.Add(val);
            }
        }
        
        public static void Convert(int[] source, List<fix> target)
        {
            foreach (var t in source)
            {
                fix val = Convert(t);
                target.Add(val);
            }
        }

        public static float RadToDeg(fix value)
        {
            return (float)value * Mathf.Rad2Deg;
        }

        public static fix AttackSpeedFactor(int attackSpeed)
        {
            fix factor = fix.One;
            if (attackSpeed > 0)
            {
                factor = (100 +  attackSpeed) * MathUtils.ZeroZeroOne;
            }
            else if (attackSpeed < 0)
            {
                factor = -(100 +  fixMath.abs(attackSpeed)) * MathUtils.ZeroZeroOne;
            }

            return factor;
        }

        public static fix Cooldown(fix baseCd, fix factor)
        {
            if (factor == fix.One || factor == fix.Zero)
            {
                return baseCd;
            }
            
            return baseCd / factor;
        }

        public static int ClampAttributeUpLimit(long power)
        {
            if (power < int.MaxValue) return (int)power;
#if UNITY_EDITOR
            Debug.LogError("战力属性超过了21亿: " + power);
#else
            Debug.LogWarning("战力属性超过了21亿: " + power);
#endif
            
            return int.MaxValue;
        }
        
        
        public static fix Convert(cfg.Fix value)
        {
            return value.Val / PrecisionFix;
        }
        
        public static fix3 Convert(cfg.Fix3 value)
        {
            return new fix3(value.X, value.Y, value.Z);
        }
      
        public static fix SignedAngle(fix3 from, fix3 to, fix3 axis)
        {
            // 1. 计算两个向量之间的无符号角度
            fix unsignedAngle = Angle(from, to);
    
            // 2. 计算叉积 (from × to)
            fix crossX = from.y * to.z - from.z * to.y;
            fix crossY = from.z * to.x - from.x * to.z;
            fix crossZ = from.x * to.y - from.y * to.x;
    
            // 3. 计算叉积与给定轴的点积，得到方向符号
            fix dotWithAxis = axis.x * crossX + axis.y * crossY + axis.z * crossZ;
            fix sign = fixMath.sign(dotWithAxis); // 使用之前实现的 sign 函数
    
            // 4. 返回带符号的角度
            return unsignedAngle * sign;
        }
        
        public static fix Angle(fix3 from, fix3 to)
        {
            // 避免除以零
            fix denominator = fixMath.sqrt(fixMath.dot(from, from) * fixMath.dot(to, to));
            if (denominator < 0)
                return fix.Zero;
    
            fix dotProduct = fixMath.dot(from.normalized, to.normalized);
    
            // 限制点积在 [-1, 1] 范围内，防止数值误差导致的反余弦域错误
            dotProduct = fixMath.clamp(dotProduct, -fix.One, fix.One);
    
            return fixMath.acos(dotProduct);
        }
    }
}