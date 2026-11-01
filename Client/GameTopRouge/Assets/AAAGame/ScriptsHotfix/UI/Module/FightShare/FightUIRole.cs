using AAAGame.ScriptsHotfix.Bridge;
using GameFramework;

namespace AAAGame.ScriptsHotfix.UI.Module.FightCommon
{
    /// <summary>缓存显示角色 HUD 所需的数据和实例。</summary>
    public sealed class FightUIRole : IReference
    {
        /// <summary>Gameplay 经 Bridge 转换后的角色显示快照。</summary>
        public GamePlayToUIBridge.RoleHudSnapshot Snapshot;
        /// <summary>角色实例编号。</summary>
        public int Id => Snapshot.RoleId;
        /// <summary>当前角色对应的 HUD。</summary>
        public FightUIRoleHud Hud;

        /// <summary>从角色快照创建 UI 资料。</summary>
        /// <param name="snapshot">角色的 HUD 显示数据。</param>
        public static FightUIRole Create(GamePlayToUIBridge.RoleHudSnapshot snapshot)
        {
            FightUIRole role = ReferencePool.Acquire<FightUIRole>();
            role.Snapshot = snapshot;
            return role;
        }

        /// <summary>重置并清理对象池实例。</summary>
        public void Clear()
        {
            Snapshot = default;
            Hud = null;
        }
    }
}
