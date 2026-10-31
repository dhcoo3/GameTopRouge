using System;
using System.Collections.Generic;
using AAAGame.ScriptsHotfix.Bridge;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Aoe;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Bullet;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Common;
using AAAGame.ScriptsHotfix.GamePlay.Logic.GameInput;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Map;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Player;
using AAAGame.ScriptsHotfix.GamePlay.Logic.ProxyManager;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Role;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Skill;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Aoe;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Bullet;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Core;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Role;
using cfg.Entity;
using Fantasy;
using Fantasy.Async;
using GameFramework;
using Unity.Mathematics;
using UnityEngine;
using Log = UnityGameFramework.Runtime.Log;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Unit
{
    /// <summary>
    /// 游戏单位处理
    /// </summary>
    public class UnitProxy:GameProxy
    {
        private int _id = GamePlayDefine.CreateId.Unit;
        
        private PlayerProxy _playerProxy;
        
        public SortedDictionary<int, RoleUnit> PlayerRole { get; } = new SortedDictionary<int,RoleUnit>();
        public SortedDictionary<int, IUnit> Units { get; } = new SortedDictionary<int,IUnit>();
        public SortedDictionary<int, IUnit> RoleUnits { get; } = new SortedDictionary<int,IUnit>();
        public SortedDictionary<int, RoleUnit> Heros { get; } = new SortedDictionary<int,RoleUnit>();
        public SortedDictionary<int, RoleUnit> Monsters { get; } = new SortedDictionary<int,RoleUnit>();
        public SortedDictionary<int, BulletUnit> Bullets { get; } = new SortedDictionary<int,BulletUnit>();
        public SortedDictionary<int, AoeUnit> Aoes { get; } = new SortedDictionary<int,AoeUnit>();
        
        private readonly SortedDictionary<int, IUnit> _waitRecycleUnits = new SortedDictionary<int,IUnit>();

        public RoleUnit MainRole;
        
        private SkillProxy _skillProxy;

        private MapProxy _mapProxy;

        private TbHero _tbHero;
        private TbMonster _tbMonster;
        
        public override async void Initialize()
        {
            try
            {
                _tbHero = await GF.LuBan.Get<TbHero>(cfg.Tables.entity_tbhero);
                _tbMonster = await GF.LuBan.Get<TbMonster>(cfg.Tables.entity_tbmonster);
            }
            catch (Exception e)
            {
                Log.Error("Initialize error = {0}",e.Message);
            }
            
            _playerProxy = GetProxy<PlayerProxy>();
            _skillProxy = GetProxy<SkillProxy>();
            _mapProxy = GetProxy<MapProxy>();
            Subscribe(GamePlayEvent.EFightLoadingFinish,FightLoadingFinish);
        }

        public override void Clear()
        { 
            base.Clear();
        }
        
        public void LoadingRole()
        {
            Log.Info("LoadingRole");
            foreach (var playerData in _playerProxy.Players)
            {
                foreach (var heroId in playerData.HeroIds)
                {
                    CreateHeroRoleUnit(heroId,playerData);
                }
            }
            
            Fire(GamePlayEvent.ECreateRoleAll);
            Fire(GamePlayEvent.ERenderAllRole);
        }
        
        public RoleUnit CreateHeroRoleUnit(int heroId,PlayerData playerData)
        {
            cfg.Entity.Hero data = _tbHero.GetOrDefault(heroId);
            if (data == null)
            {
                Log.Warning("CreateHeroRoleUnit Null {0}",heroId);
                return null;
            }

            if (!_mapProxy.TryGetHeroSpawnWorldPosition(playerData.Pos, out fix3 spawnPosition))
            {
                Log.Error("创建英雄失败：地图出生点配置无效或玩家出生位无效，玩家={0}，出生位={1}", playerData.Id, playerData.Pos);
                return null;
            }

            int id = playerData.Id;
            RoleHero roleData = RoleHero.Create(playerData,id,data);
            // 出生配置定义碰撞圆心位置；渲染根节点需要反向减去偏移。
            roleData.InitPosition = spawnPosition - roleData.CollisionOffset;
            // RoleBehaviour captures InitPosition during RoleUnit.Create, so assign it first.
            RoleUnit roleUnit = RoleUnit.Create(roleData);
            
            Units.Add(id, roleUnit);
            Heros.Add(id, roleUnit);
            RoleUnits.Add(id, roleUnit);
            PlayerRole.Add(playerData.Id,roleUnit);
            GamePlayToUIBridge.Instance.CreateRole(roleUnit);
            return roleUnit;
        }
        
        public RoleUnit CreateMonsterRoleUnit(int monsterId)
        {
            cfg.Entity.Monster data = _tbMonster.GetOrDefault(monsterId);
            if (data == null)
            {
                Log.Info("CreateMonsterRoleUnit Null {0}",monsterId);
                return null;
            }
            
            _id++;
            RoleMonster roleData = RoleMonster.Create(_id,data);
            
            RectInt rect = _mapProxy.MapInfo.isometric
                ? new RectInt(5, 7, 10, 12)
                : new RectInt(0, 0, 9, 10);
            fix3 pos = _mapProxy.MapInfo.GetRandomPosForCharacter(rect, roleData.Prop.BodyRadius);
            roleData.InitPosition = pos;
            
            RoleUnit roleUnit = RoleUnit.Create(roleData);
            Units.Add(_id, roleUnit);
            Monsters.Add(_id, roleUnit);
            RoleUnits.Add(_id, roleUnit);
            GamePlayToUIBridge.Instance.CreateRole(roleUnit);
            return roleUnit;
        }
        
        public BulletUnit CreateBulletUnit(BulletData bulletData)
        {
            _id++;
            bulletData.BulletId = _id;
            BulletUnit bulletUnit = BulletUnit.Create(bulletData);
            Units.Add(_id, bulletUnit);
            Bullets.Add(_id, bulletUnit);
            Fire(GamePlayEvent.ERenderBullet,bulletUnit);
            return bulletUnit;
        }

        public AoeUnit CreateAoeUnit(AoeData aoeData)
        {
            _id++;
            aoeData.AoeId = _id;
            AoeUnit aoeUnit = AoeUnit.Create(aoeData);
            Units.Add(_id, aoeUnit);
            Aoes.Add(_id, aoeUnit);
            Fire(GamePlayEvent.ERenderAoe,aoeUnit);
            return aoeUnit;
        }
        
        public IUnit GetRoleUnit(int id)
        {
            return Units.GetValueOrDefault(id);
        }

        public void RemoveRoleUnit(int id)
        {
            if (Units.TryGetValue(id, out IUnit roleUnit))
            {
                _waitRecycleUnits.TryAdd(id, roleUnit);
            }
        }

        private void CheckDestroy()
        {
            if (_waitRecycleUnits.Count > 0)
            {
                foreach (int id in _waitRecycleUnits.Keys)
                {
                    if (Units.TryGetValue(id, out IUnit unit))
                    {
                        Units.Remove(id);
                
                        if (unit is RoleUnit roleUnit)
                        {
                            Heros.Remove(id);
                            Monsters.Remove(id);
                            RoleUnits.Remove(id);
                            GamePlayToUIBridge.Instance.RoleDie(roleUnit);
                            FireNow(GamePlayEvent.EStopRenderRole,id);
                            ReferencePool.Release(roleUnit);
                        }
                        else if (unit is BulletUnit bulletUnit)
                        {
                            Bullets.Remove(id);
                            FireNow(GamePlayEvent.EStopRenderBullet,id);
                            ReferencePool.Release(bulletUnit);
                        }
                        else if (unit is AoeUnit aoeUnit)
                        {
                            Aoes.Remove(id);
                            FireNow(GamePlayEvent.EStopRenderAoe,id);
                            ReferencePool.Release(aoeUnit);
                        }
                    }
                }
                
                _waitRecycleUnits.Clear();
            }
        }

        public override void LogicUpdate(fix deltaTime)
        {
            CheckDestroy();
            
            foreach (var unit in Units.Values)
            {
                unit.LogicUpdate(deltaTime);
            }
        }

        public void OperatorMainRole(InputObj inputObj)
        {
            if (PlayerRole.TryGetValue(inputObj.PlayerId, out RoleUnit playerUnit))
            {
                Log.Info("inputObj.PlayerId:{0} {1} {2} {3}",GF.UserId,playerUnit.RoleId,inputObj.PlayerId,playerUnit.Data.OperateId);
                playerUnit.SetOperator(inputObj);
            }
        }

        /// <summary>
        /// 在战斗资源加载完成后初始化角色技能，并只为单机模式生成随机出生点。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">战斗加载完成事件。</param>
        private void FightLoadingFinish(object sender, GameEvent e)
        {
            if (GamePlayFacade.Instance.IsRoomMode)
            {
                foreach (RoleUnit roleUnit in PlayerRole.Values)
                {
                    LearnDefaultSkills(roleUnit);
                }
            }

            if (!PlayerRole.TryGetValue(GF.UserId, out RoleUnit playerUnit))
            {
                return;
            }

            MainRole = playerUnit;
            if (!GamePlayFacade.Instance.IsRoomMode)
            {
                RectInt data = _mapProxy.MapInfo.isometric
                    ? new RectInt(7, 10, 6, 6)
                    : new RectInt { width = 5, height = 5 };
                fix3 position = _mapProxy.MapInfo.GetRandomPosForCharacter(data, MainRole.Data.Prop.BodyRadius);
                MainRole.RoleBehaviour.ApplyPosition(position);
                LearnDefaultSkills(MainRole);
            }

            Fire(GamePlayEvent.ECameraFocus, MainRole);
        }

        /// <summary>
        /// 为角色配置当前联调使用的固定技能组，确保房间内每个客户端状态一致。
        /// </summary>
        /// <param name="roleUnit">需要配置技能的角色单位。</param>
        private void LearnDefaultSkills(RoleUnit roleUnit)
        {
            roleUnit.LearnSkill(_skillProxy.GetSkillModel(100001));
            roleUnit.LearnSkill(_skillProxy.GetSkillModel(100002));
            roleUnit.LearnSkill(_skillProxy.GetSkillModel(100004));
        }

    }
}
