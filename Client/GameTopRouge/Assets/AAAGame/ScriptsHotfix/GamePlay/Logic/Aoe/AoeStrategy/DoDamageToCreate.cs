using System.Collections.Generic;
using AAAGame.ScriptsHotfix.Common;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Damage;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Effect;
using AAAGame.ScriptsHotfix.GamePlay.Logic.ProxyManager;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Aoe;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Core;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Role;
using UnityEngine;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Aoe.AoeStrategy
{
    /// <summary>AOE 效果创建时的伤害策略。</summary>
    public partial class AoeStrategyManager
    {
        /// <summary>AOE 创建时，根据阵营配置为范围内的有效角色分别登记伤害。</summary>
        /// <param name="aoe">包含施法者、创建时属性快照和伤害配置的范围效果。</param>
        /// <param name="characters">范围内角色，按集合顺序登记伤害。</param>
        /// <param name="bullets">范围内子弹，当前伤害策略不处理。</param>
        private static void DoDamageToCreate(AoeUnit aoe,List<IUnit> characters,List<IUnit> bullets)
        {
            if (aoe.Data.model.onCreateParams is cfg.Skill.DoDamageToCreate doDamageToEnterCha)
            {
                fix damageTimes = MathUtils.Convert(doDamageToEnterCha.DamageTimes);
                bool toFoe = doDamageToEnterCha.ToFoe;
                bool toAlly = doDamageToEnterCha.ToAlly;
                bool hurtAction = doDamageToEnterCha.HurtAction;
                string effect = doDamageToEnterCha.Effect;
                string bp = doDamageToEnterCha.BpName;

                // 多目标只共享数值，每次命中独占一个可回收对象，避免首个目标结算后清空后续伤害。
                int damage = fixMath.roundToInt(doDamageToEnterCha.BaseDamage * (aoe.Data.propWhileCreate.Attack * damageTimes));

                int side = -1;
                if (aoe.Data.caster != null &&aoe.Data.caster is RoleUnit role)
                { 
                    side = role.Data.Side;
                }

                for (int i = 0; i < characters.Count; i++){
                    if (characters[i] is RoleUnit unit 
                        && unit.IsDeath() == false 
                        && ((toFoe && side != unit.Data.Side) 
                            || (toAlly && side == unit.Data.Side)))
                    {
                        fix3 chaToAoe = unit.Behaviour.Position - aoe.Behaviour.Position;
                        
                        GameProxyManger.Instance.GetProxy<DamageProxy>().DoDamage(
                            aoe.Data.caster,
                            unit,
                            DamageVal.Create(damage),
                            fixMath.atan2(chaToAoe.x, chaToAoe.z) * 180 / fixMath.PI,
                            0.05f,
                            new DamageDefine.DamageInfoTag[] { DamageDefine.DamageInfoTag.directDamage, }
                        );

                        if (hurtAction)
                        {
                           //unit.Play("Hurt");
                        }

                        if (effect != "")
                        {
                           GameProxyManger.Instance.GetProxy<EffectProxy>().PlayToPositionRecycle1(
                               GF.AssetBridge.GetEffect(effect),unit.Behaviour.Position);
                        }
                    }
                }
            }
        }
    }
}
