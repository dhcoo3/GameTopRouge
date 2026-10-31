using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Aoe;
using AAAGame.ScriptsHotfix.GamePlay.Render.Entity;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.GamePlay.Render.Aoe
{
    public class AoeEntity:EntityRender
    {
        AoeUnit _aoeUnit;
        public Vector3 Position
        {
            get
            {
                return transform.position;
            }
            set
            {
                transform.position = value;
            }
        }
        
        private Vector3 _tmpVector3 = Vector3.zero;
        
        protected override void OnShow(object userData)
        {
            AssetInfo assetInfo = (AssetInfo)userData;
            _aoeUnit = assetInfo.UserData as AoeUnit;
           
            if (_aoeUnit == null)
            {
                Log.Error("aoeUnit is invalid.");
                return;
            }

            _tmpVector3.x = _aoeUnit.Behaviour.Position.x;
            _tmpVector3.y = _aoeUnit.Behaviour.Position.y;
            _tmpVector3.z = _aoeUnit.Behaviour.Position.z;
            
            Position = _tmpVector3;
            transform.rotation = _aoeUnit.Behaviour.Rotation;
            _aoeUnit.HasEntity = true;
            base.OnShow(userData);
        }

        public override void LogicUpdate(fix deltaTime)
        {
            if (_aoeUnit == null) return;
            //将逻辑层坐标同步到渲染层
            _tmpVector3.x = _aoeUnit.Behaviour.Position.x;
            _tmpVector3.y = _aoeUnit.Behaviour.Position.y;
            _tmpVector3.z = _aoeUnit.Behaviour.Position.z;
            
            transform.position = _tmpVector3;
            transform.rotation = _aoeUnit.Behaviour.Rotation;
            base.LogicUpdate(deltaTime);
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            _aoeUnit = null;
            base.OnHide(isShutdown, userData);
        }

        protected override void OnRecycle()
        {
            _aoeUnit = null;
            base.OnRecycle();
        }
        
#if UNITY_EDITOR
        private fix range = 5f; // 检测范围半径
        private fix minAngle = 0f; // 最小角度（以右为0度）
        private fix maxAngle = 90f; // 最大角度（以右为0度）

        private void OnDrawGizmosSelected()
        {
            if(_aoeUnit == null) return;
            if (_aoeUnit.Data.degree == 0) return;
            
            // 计算角度范围（以角色朝向为0度，对称分布）
            fix totalAngle = _aoeUnit.Data.degree;
            minAngle = -totalAngle / 2f;  // 左侧边界角度
            maxAngle = totalAngle / 2f;   // 右侧边界角度
            range = _aoeUnit.Data.radius;
            
            Gizmos.color = new Color(0, 1, 0, 0.3f); // 半透明绿色

            // 获取角色当前朝向（考虑Rotation）
            fix3 characterForward = new fix3(transform.right.x,transform.right.y,transform.right.z); // 2D中通常用right作为朝向
            fix characterRotation = transform.eulerAngles.z; // 获取Z轴旋转角度
            
            fix3 center = new fix3(transform.position.x,transform.right.y,transform.right.z);
            fix3 prevPoint = center + GetRotatedDirection(minAngle, characterRotation) * range;
            fix segments = fixMath.max(20, fixMath.floorToInt(totalAngle / 2f)); // 动态分段

            // 绘制扇形填充区域
            for (int i = 1; i <= segments; i++)
            {
                fix angle = fixMath.lerp(minAngle, maxAngle, i / (fix)segments);
                fix3 currentPoint = center + GetRotatedDirection(angle, characterRotation) * range;
               
                // 绘制扇形三角形
                Gizmos.DrawLine((Vector3)center, (Vector3)currentPoint);
                Gizmos.DrawLine((Vector3)prevPoint, (Vector3)currentPoint);
                prevPoint = currentPoint;
            }

            // 绘制弧线边框
            Gizmos.color = Color.green;
            prevPoint = center + GetRotatedDirection(minAngle, characterRotation) * range;
            for (int i = 1; i <= segments; i++)
            {
                fix angle = fixMath.lerp(minAngle, maxAngle, i / (fix)segments);
                fix3 currentPoint = center + GetRotatedDirection(angle, characterRotation) * range;
                Gizmos.DrawLine((Vector3)prevPoint, (Vector3)currentPoint);
                prevPoint = currentPoint;
            }

            // 绘制边界线
            Gizmos.DrawLine((Vector3)center, (Vector3)(center + GetRotatedDirection(minAngle, characterRotation) * range));
            Gizmos.DrawLine((Vector3)center, (Vector3)(center + GetRotatedDirection(maxAngle, characterRotation) * range));
            
            // 绘制角度参考线
            Gizmos.color = Color.red;
            Gizmos.DrawLine((Vector3)center, (Vector3)(center + characterForward * range)); // 角色当前朝向
            
            Gizmos.color = Color.blue;
            Gizmos.DrawLine((Vector3)center, (Vector3)(center + GetRotatedDirection(minAngle, characterRotation) * range)); // 左侧边界
            Gizmos.DrawLine((Vector3)center, (Vector3)(center + GetRotatedDirection(maxAngle, characterRotation) * range)); // 右侧边界
        }

        // 辅助方法：根据角度获取方向向量（考虑角色旋转）
        private fix3 GetRotatedDirection(fix angle, fix characterRotation)
        {
            // 总角度 = 相对角度 + 角色旋转角度
            fix totalAngle = angle + characterRotation;
            fix rad = fixMath.rad(totalAngle);
            return new fix3(fixMath.cos(rad), fixMath.sin(rad), 0);
        }
#endif
    }
}