using System;
using AAAGame.ScriptsHotfix.Common;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Aoe;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Buff;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Bullet;
using AAAGame.ScriptsHotfix.GamePlay.Logic.ProxyManager;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Role;
using AAAGame.ScriptsHotfix.GamePlay.Logic.TimeLine;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Core;
using cfg.Skill;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Skill
{
    public class SkillProxy:GameProxy
    {
        private TbSkillDef _tbSkillDef;
        private TbTimeLineDef _tbTimeLineDef;
        private TbSkillBulletDef _tbSkillBulletDef;
        private TbBulletDef _tbBulletDef;
        private TbBuffDef _tbBuffDef;
        private TbSkillAoeDef _tbSkillAoeDef;
        private TbAoeDef _tbAoeDef;
        
        public override async void Initialize()
        {
            try
            {
                _tbSkillDef = await GF.LuBan.Get<TbSkillDef>(cfg.Tables.skill_tbskilldef);
                _tbTimeLineDef = await GF.LuBan.Get<TbTimeLineDef>(cfg.Tables.skill_tbtimelinedef);
                _tbSkillBulletDef = await GF.LuBan.Get<TbSkillBulletDef>(cfg.Tables.skill_tbskillbulletdef);
                _tbBulletDef = await GF.LuBan.Get<TbBulletDef>(cfg.Tables.skill_tbbulletdef);
                _tbBuffDef = await GF.LuBan.Get<TbBuffDef>(cfg.Tables.skill_tbbuffdef);
                _tbSkillAoeDef = await GF.LuBan.Get<TbSkillAoeDef>(cfg.Tables.skill_tbskillaoedef);
                _tbAoeDef = await GF.LuBan.Get<TbAoeDef>(cfg.Tables.skill_tbaoedef);
            }
            catch (Exception e)
            {
                Log.Error("Initialize error = {0}",e.Message);
            }
        }
        
        public SkillModel GetSkillModel(int id)
        {
            SkillDef skillDef = _tbSkillDef.GetOrDefault(id);
            if (skillDef == null)
            {
                Log.Error("skillDef is null id = {0}",id);
                return null;
            }
            
            ChaResource cost = new ChaResource(skillDef.Cost.Hp, skillDef.Cost.Ammo, skillDef.Cost.Stamina);
            ChaResource condition = new ChaResource(skillDef.Condition.Hp, skillDef.Condition.Ammo, skillDef.Condition.Stamina);

            AddBuffInfo[] addBuffInfos = new AddBuffInfo[skillDef.AddBuff.Count];
            for (int i = 0; i < skillDef.AddBuff.Count; i++)
            {
                int buffId = skillDef.AddBuff[i];
                addBuffInfos[i] = GetAddBuffInfo(buffId);
            }
            
            SkillModel skillModel = new SkillModel(id,cost,condition,GetTimeline(skillDef.TimeLine),addBuffInfos);
            return skillModel;
        }

        public TimelineModel GetTimeline(int id)
        {
            TimeLineDef timeLineDef = _tbTimeLineDef.GetOrDefault(id);
            if (timeLineDef == null)
            {
                Log.Error("timeLineDef is null id = {0}",id);
                return null;
            }

            TimelineNode[] timelineNodes = new TimelineNode[timeLineDef.TimeLineParams.Count];
            for (int i = 0; i < timeLineDef.TimeLineParams.Count; i++)
            {
                timelineNodes[i] = new TimelineNode(timeLineDef.TimeLineParams[i]);
            }

            TimelineModel model = new TimelineModel(id, timelineNodes, MathUtils.Convert(timeLineDef.Duration),TimeLineGoTo.Null);
            return model;
        }

        public BulletData GetBulletData(int id)
        {
            SkillBulletDef skillBulletDef = _tbSkillBulletDef.GetOrDefault(id);
            if (skillBulletDef == null)
            {
                Log.Error("skillBulletDef is null id = {0}",id);
                return null;
            }

            BulletModel bulletModel = GetBulletModel(skillBulletDef.BulletData);

            BulletData bulletData = BulletData.Create(
                bulletModel, null,
                MathUtils.Convert(skillBulletDef.FirePos),
                MathUtils.Convert(skillBulletDef.Degree), 
                MathUtils.Convert(skillBulletDef.Speed),
                MathUtils.Convert(skillBulletDef.Duration),
                fix.Zero
            );

            return bulletData;
        }

        public BulletModel GetBulletModel(int id)
        {
            BulletDef bulletDef = _tbBulletDef.GetOrDefault(id);
            if (bulletDef == null)
            {
                Log.Error("bulletDef is null id = {0}",id);
                return null;
            }

            BulletModel bulletModel = BulletModel.Create(bulletDef.Id,
                bulletDef.Prefab,
                bulletDef.OnCreate,
                bulletDef.OnHit,
                bulletDef.OnRemoved,
                BulletDefine.MoveType.fly,
                bulletDef.RemoveOnObstacle,
                MathUtils.Convert(bulletDef.Radius),
                bulletDef.HitTimes,
                MathUtils.Convert(bulletDef.SameTargetDelay),
                bulletDef.HitFoe,
                bulletDef.HitAlly);

            return bulletModel;
        }

        public AddBuffInfo GetAddBuffInfo(int buffId)
        {
            BuffDef buffDef = _tbBuffDef.GetOrDefault(buffId);
            if(buffDef == null)
            {
                Log.Error("buffDef is null id = {0}",buffId);
                return null;
            }

            BuffModel buffModel = GetBuffModel(buffId);

            return AddBuffInfo.Create(buffModel, null, 
                null, 
                1,  
                MathUtils.Convert(buffDef.Duration), 
                true, 
                buffDef.Permanent);
        }

        public BuffModel GetBuffModel(int buffId)
        {
            BuffDef buffDef = _tbBuffDef.GetOrDefault(buffId);
            if(buffDef == null)
            {
                Log.Error("buffDef is null id = {0}",buffId);
                return null;
            }
            
            RoleState roleState = new RoleState(buffDef.BuffAddState.CanMove,
                buffDef.BuffAddState.CanRotate,
                buffDef.BuffAddState.CanUseSkill);

            ChaProperty[] chaProperties = new ChaProperty[buffDef.BuffPropMod.Length];
            
            for (int j = 0; j < buffDef.BuffPropMod.Length; j++)
            {
                BuffPropMod buffPropMod = buffDef.BuffPropMod[j];
                chaProperties[j] = new ChaProperty(buffPropMod.MoveSpeed, 
                    buffPropMod.Hp,
                    buffPropMod.Ammo,
                    buffPropMod.Attack,
                    buffPropMod.ActionSpeed, 
                    MathUtils.Convert(buffPropMod.BodyRadius),
                    MathUtils.Convert(buffPropMod.HitRadius));
            }
            
            BuffModel buffModel = BuffModel.Create(buffId, "", 
                buffDef.Tag, 
                buffDef.Priority, 
                buffDef.MaxStack,
                buffDef.TickTime, 
                buffDef.OnOccur,
                buffDef.OnRemoved, 
                buffDef.OnTick, 
                buffDef.OnCast, 
                buffDef.OnHit,
                buffDef.BeHurt, 
                buffDef.OnKill, 
                buffDef.BeKilled,
                roleState,
                chaProperties);

            return buffModel;
        }

        public AoeData GetAoeData(int skillAoeId,IUnit caster)
        {
            SkillAoeDef  skillAoeDef = _tbSkillAoeDef.GetOrDefault(skillAoeId);
            if (skillAoeDef == null)
            {
                Log.Error("skillAoeDef is null id = {0}",skillAoeId);
                return null;
            }

            AoeModel model = GetAodeModel(skillAoeDef.AoeDataId);
            if (model == null)
            {
                return null;
            }
        
            AoeData aoeData = AoeData.Create(model, 
                caster, 
                fix3.zero, 
                MathUtils.Convert(skillAoeDef.Radius),
                MathUtils.Convert(skillAoeDef.Duration), 
                MathUtils.Convert(skillAoeDef.Degree));
            
            Log.Info("创建AOE: {0} 角度:{1} 半径:{2}", aoeData.AoeId,aoeData.degree,aoeData.radius);
            return aoeData;
        }

        private AoeModel GetAodeModel(int aoeId)
        {
            AoeDef aoeDef = _tbAoeDef.GetOrDefault(aoeId);
            if (aoeDef == null)
            {
                Log.Error("aoeDef is null id = {0}",aoeId);
                return null;
            }
            
            AoeModel aoeModel = AoeModel.Create(aoeDef.Id,
                aoeDef.Prefab,
                aoeDef.Tags,
                MathUtils.Convert(aoeDef.TickTime),
                aoeDef.RemoveOnObstacle,
                aoeDef.OnCreate,
                aoeDef.OnRemoved,
                aoeDef.OnTick,
                aoeDef.OnChaEnter,
                aoeDef.OnChaLeave,
                aoeDef.OnBulletEnter,
                aoeDef.OnBulletLeave);
            
            return aoeModel;
        }
    }
}