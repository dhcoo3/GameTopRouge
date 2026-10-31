using System.Collections.Generic;
using AAAGame.ScriptsHotfix.Bridge;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Buff;
using AAAGame.ScriptsHotfix.GamePlay.Logic.ProxyManager;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Role;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Core;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Role;
using GameFramework;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Damage
{
    /// <summary>按登记顺序处理伤害、治疗及其 Buff 回调，并负责伤害对象的回收。</summary>
    public class DamageProxy : GameProxy
    {
        /// <summary>尚未结算的伤害队列；回调产生的新伤害追加到队尾。</summary>
        private readonly Queue<DamageInfo> _damageInfos = new Queue<DamageInfo>();

        /// <summary>初始化伤害代理，队列在首次使用时已准备完成。</summary>
        public override void Initialize()
        {
        }

        /// <summary>逐条结算并回收伤害；先出队，避免异常后重复扣血。</summary>
        /// <param name="deltaTime">本次固定逻辑步长，单位为秒。</param>
        public override void LogicUpdate(fix deltaTime)
        {
            while (_damageInfos.Count > 0)
            {
                DamageInfo damageInfo = _damageInfos.Dequeue();
                try
                {
                    DealWithDamage(damageInfo);
                }
                finally
                {
                    ReferencePool.Release(damageInfo);
                }
            }
        }

        /// <summary>退出战斗时回收未结算伤害，避免代理复用后继续结算上一局事件。</summary>
        public override void Clear()
        {
            while (_damageInfos.Count > 0)
            {
                ReferencePool.Release(_damageInfos.Dequeue());
            }

            base.Clear();
        }

        /// <summary>先执行伤害修正，再固定结算值、触发击杀回调、扣血并添加 Buff。</summary>
        /// <param name="damageInfo">当前队列持有的伤害信息，攻击来源允许为空。</param>
        private void DealWithDamage(DamageInfo damageInfo)
        {
            RoleUnit defender = damageInfo.defender as RoleUnit;
            if (defender?.Data == null || defender.RoleState.IsDeath) return;

            RoleUnit attacker = damageInfo.attacker as RoleUnit;
            List<BuffObj> attackerBuffs = attacker?.Data?.Buffs;
            List<BuffObj> defenderBuffs = defender.Data.Buffs;

            if (attackerBuffs != null)
            {
                for (int i = 0; i < attackerBuffs.Count; i++)
                {
                    BuffObj buff = attackerBuffs[i];
                    buff.model.onHit?.Invoke(buff, ref damageInfo, damageInfo.defender);
                }
            }

            for (int i = 0; i < defenderBuffs.Count; i++)
            {
                BuffObj buff = defenderBuffs[i];
                buff.model.onBeHurt?.Invoke(buff, ref damageInfo, damageInfo.attacker);
            }

            // 击杀预判和实际扣血使用同一个结算值，暴击不随查询次数重新随机。
            bool isHeal = damageInfo.isHeal();
            int damageValue = damageInfo.DamageValue(isHeal);
            if (isHeal || defender.RoleState.ImmuneTime <= fix.Zero)
            {
                bool isLethal = !isHeal && damageValue > 0 && damageValue >= defender.Data.Resource.hp;
                if (isLethal)
                {
                    TriggerKillCallbacks(attackerBuffs, defenderBuffs, damageInfo);
                }

                defender.ModResource(new ChaResource(-damageValue));
                GamePlayToUIBridge.Instance.UpdateHp(defender);
                GamePlayToUIBridge.Instance.PopUpNumberOnCharacter(defender, damageValue, isHeal);
            }

            ApplyPendingBuffs(damageInfo);
        }

        /// <summary>沿用扣血前的击杀回调顺序；回调接收已经确定结算值的伤害事件。</summary>
        /// <param name="attackerBuffs">攻击者的 Buff；无攻击者时允许为空。</param>
        /// <param name="defenderBuffs">受击者的 Buff。</param>
        /// <param name="damageInfo">已完成伤害修正及暴击判定的事件。</param>
        private static void TriggerKillCallbacks(List<BuffObj> attackerBuffs, List<BuffObj> defenderBuffs,
            DamageInfo damageInfo)
        {
            if (attackerBuffs != null)
            {
                for (int i = 0; i < attackerBuffs.Count; i++)
                {
                    BuffObj buff = attackerBuffs[i];
                    buff.model.onKill?.Invoke(buff, damageInfo, damageInfo.defender);
                }
            }

            for (int i = 0; i < defenderBuffs.Count; i++)
            {
                BuffObj buff = defenderBuffs[i];
                buff.model.onBeKilled?.Invoke(buff, damageInfo, damageInfo.attacker);
            }
        }

        /// <summary>按请求中记录的实际目标添加 Buff，跳过空请求和已经死亡或回收的目标。</summary>
        /// <param name="damageInfo">包含待执行 Buff 请求的伤害信息。</param>
        private static void ApplyPendingBuffs(DamageInfo damageInfo)
        {
            for (int i = 0; i < damageInfo.addBuffs.Count; i++)
            {
                AddBuffInfo buffInfo = damageInfo.addBuffs[i];
                RoleUnit target = buffInfo?.target as RoleUnit;
                if (buffInfo?.buffModel == null || target?.Data == null || target.RoleState.IsDeath) continue;

                target.AddBuff(buffInfo);
            }
        }

        /// <summary>登记伤害或治疗，并接管数值对象的回收；每个请求必须使用独立数值对象。</summary>
        /// <param name="attacker">攻击来源，环境伤害时允许为空。</param>
        /// <param name="target">接收伤害或治疗的角色；无效目标的事件会在结算时回收。</param>
        /// <param name="damageVal">本次请求独占的可回收数值对象，不允许为空。</param>
        /// <param name="damageDegree">伤害入射角度，单位为度。</param>
        /// <param name="criticalRate">基础暴击率，零及以下不暴击，一及以上必定暴击。</param>
        /// <param name="tags">伤害类型标签，允许为空。</param>
        public void DoDamage(IUnit attacker, IUnit target, DamageVal damageVal, fix damageDegree, fix criticalRate,
            DamageDefine.DamageInfoTag[] tags)
        {
            if (damageVal == null)
            {
                Log.Warning("伤害数值为空，忽略伤害请求。");
                return;
            }

            _damageInfos.Enqueue(DamageInfo.Create(attacker, target, damageVal, damageDegree, criticalRate, tags));
        }
    }
}
