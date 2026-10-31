using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Role;
using AAAGame.ScriptsHotfix.UI.Core;
using AAAGame.ScriptsHotfix.UI.Module.FightMainChapter;
using AAAGame.ScriptsHotfix.UI.Module.Login;
using cfg.Fight;
using cfg.UI;
using Cysharp.Threading.Tasks;
using System;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.UI.Module.FightShare
{
    #if ENABLE_OBFUZ
    [Obfuz.ObfuzIgnore]
    #endif
    public class FightShareController : ModuleController<FightShareController>,IController
    {
        public override void RegisterEvent()
        {
            Subscribe(FightShareConst.Event.ECreateRole,ECreateRoleHandler);
            base.RegisterEvent();
        }

        private void ECreateRoleHandler(object sender, GameEvent e)
        {
            RoleUnit roleData = e.GetParam1<RoleUnit>();
            FightShareModel.Instance.AddNewRole(roleData);
        }

        /// <summary>
        /// 记录战斗状态并启动主战斗界面与战斗覆盖层。
        /// </summary>
        /// <param name="dungeonType">本次进入的副本类型。</param>
        public void EnterFight(DungeonType dungeonType)
        {
            if (FightShareModel.Instance.FightStart)
            {
                Log.Warning("战斗已经开始,不能再次进入：当前 {0}  请求进入 {1}", FightShareModel.Instance.FightDungeonType,dungeonType);
                return;
            }

            FightShareModel.Instance.FightStart = true;
            FightShareModel.Instance.FightDungeonType = dungeonType;

            EnterFightAsync(dungeonType, FightShareModel.Instance.EntryVersion).Forget();
        }

        /// <summary>
        /// 等待主战斗入口及战斗 HUD、伤害层依次打开。
        /// </summary>
        /// <param name="dungeonType">本次进入的副本类型。</param>
        /// <param name="entryVersion">开局时的认证界面代次，退出登录后立即失效。</param>
        private async UniTask EnterFightAsync(DungeonType dungeonType, int entryVersion)
        {
            try
            {
                switch (dungeonType)
                {
                    case DungeonType.Main:
                        await FightMainChapterController.Instance.EnterFightAsync();
                        break;
                }

                if (!IsEntryCurrent(entryVersion)) return;
                await UIService.Instance.OpenAsync<OverlayFightHudUI>(ModuleType.FightCommon,
                    FightShareConst.OverlayFightHudUI);
                if (!IsEntryCurrent(entryVersion)) return;
                await UIService.Instance.OpenAsync<OverlayFightDamageUI>(ModuleType.FightCommon,
                    FightShareConst.OverlayFightDamageUI);
            }
            catch (Exception exception)
            {
                Log.Error($"进入战斗界面失败，副本类型：{dungeonType}，原因：{exception}");
            }
        }

        /// <summary>认证结束后停止旧开局任务继续打开界面。</summary>
        /// <param name="entryVersion">开局时记录的版本。</param>
        /// <returns>本次开局仍有效时返回真。</returns>
        private static bool IsEntryCurrent(int entryVersion) => LoginModel.Instance.IsAuthenticated &&
            FightShareModel.Instance.FightStart && FightShareModel.Instance.EntryVersion == entryVersion;
    }
}
