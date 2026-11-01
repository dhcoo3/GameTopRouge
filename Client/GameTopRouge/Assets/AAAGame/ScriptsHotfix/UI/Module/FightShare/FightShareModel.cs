using System.Collections.Generic;
using AAAGame.ScriptsHotfix.Bridge;
using AAAGame.ScriptsHotfix.UI.Core;
using AAAGame.ScriptsHotfix.UI.Module.FightCommon;
using cfg.Fight;

namespace AAAGame.ScriptsHotfix.UI.Module.FightShare
{
    #if ENABLE_OBFUZ
    [Obfuz.ObfuzIgnore]
    #endif
    /// <summary>当前战斗 UI 的角色引用与开局状态。</summary>
    public class FightShareModel : ModuleModel<FightShareModel>,IModel
    {
        /// <summary>当前战斗使用的 UI 角色索引。</summary>
        private readonly Dictionary<int,FightUIRole> _fightUiRoles = new Dictionary<int,FightUIRole>();
        
        /// <summary>当前战斗的 UI 角色集合。</summary>
        public Dictionary<int,FightUIRole> FightUiRoles => _fightUiRoles;

        /// <summary>当前是否已开始战斗或正在加载战斗界面。</summary>
        public bool FightStart = false;
        /// <summary>当前副本类型。</summary>
        public DungeonType FightDungeonType = DungeonType.Main;
        /// <summary>登录失效时递增，阻止旧的开局异步任务继续打开战斗覆盖界面。</summary>
        public int EntryVersion { get; private set; }

        /// <summary>认证结束后回收本局 UI 角色引用，允许再次登录进入主线。</summary>
        public void ResetAfterLogout()
        {
            FightStart = false;
            EntryVersion++;
            foreach (var role in _fightUiRoles.Values)
            {
                role.Snapshot = default;
                role.Hud = null;
                GameFramework.ReferencePool.Release(role);
            }
            _fightUiRoles.Clear();
        }
        
        /// <summary>为本局角色建立 UI 资料。</summary>
        /// <param name="roleData">玩法层已经创建的角色。</param>
        public void AddNewRole(GamePlayToUIBridge.RoleHudSnapshot roleData)
        {
            FightUIRole uiRole = FightUIRole.Create(roleData);
            if (!_fightUiRoles.TryAdd(uiRole.Id, uiRole))
                GameFramework.ReferencePool.Release(uiRole);
        }
    }
}
