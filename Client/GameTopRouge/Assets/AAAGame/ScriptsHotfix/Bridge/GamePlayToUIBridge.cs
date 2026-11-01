using System.Collections.Generic;
using System.Threading;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Role;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Core;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Role;
using AAAGame.ScriptsHotfix.UI.Module.Dialogue;
using AAAGame.ScriptsHotfix.UI.Module.FightCommon;
using AAAGame.ScriptsHotfix.UI.Module.FightShare;
using AAAGame.ScriptsHotfix.UI.Module.Login;
using AAAGame.ScriptsHotfix.UI.Module.Quest;
using Cysharp.Threading.Tasks;
using Fantasy;
using GameFramework;
using UnityEngine;
using UnityEngine.UI;

namespace AAAGame.ScriptsHotfix.Bridge
{
    /// <summary>Gameplay 通知 UI、请求界面表现的唯一入口。</summary>
    public sealed class GamePlayToUIBridge : Singleton<GamePlayToUIBridge>
    {
        private readonly AAAGameEventHelper _eventHelper = ReferencePool.Acquire<AAAGameEventHelper>();

        /// <summary>通知 UI 创建角色 HUD，并仅传递显示所需的角色快照。</summary>
        /// <param name="roleUnit">Gameplay 创建的角色。</param>
        public void CreateRole(RoleUnit roleUnit)
        {
            _eventHelper.Fire(FightShareConst.Event.ECreateRole, CreateSnapshot(roleUnit));
        }

        /// <summary>更新指定角色在 UI 中的位置。</summary>
        /// <param name="id">角色编号。</param>
        /// <param name="position">世界坐标。</param>
        public void UpdatePos(int id, Vector3 position)
        {
            _eventHelper.Fire(FightShareConst.Event.EUpdatePos, id, position);
        }

        /// <summary>通知 UI 刷新角色生命值。</summary>
        /// <param name="roleUnit">Gameplay 角色。</param>
        public void UpdateHp(RoleUnit roleUnit)
        {
            _eventHelper.Fire(FightShareConst.Event.EUpdateHp, CreateSnapshot(roleUnit));
        }

        /// <summary>通知 UI 角色已经死亡。</summary>
        /// <param name="roleUnit">Gameplay 角色。</param>
        public void RoleDie(RoleUnit roleUnit)
        {
            RoleHudSnapshot snapshot = CreateSnapshot(roleUnit);
            _eventHelper.Fire(FightShareConst.Event.ERoleDie, snapshot);
        }

        /// <summary>在角色位置显示伤害或治疗数字。</summary>
        /// <param name="roleUnit">承受伤害或治疗的角色。</param>
        /// <param name="damageValue">显示的数值。</param>
        /// <param name="isHeal">是否为治疗数值。</param>
        public void PopUpNumberOnCharacter(RoleUnit roleUnit, int damageValue, bool isHeal)
        {
            fix3 position = roleUnit.Behaviour.Position;
            _eventHelper.Fire(FightShareConst.Event.EPopUpNumber,
                new Vector3((float)position.x, (float)position.y, (float)position.z), damageValue, isHeal);
        }

        /// <summary>在指定地图加载对应账号任务会话及界面。</summary>
        /// <param name="mapId">当前地图编号。</param>
        /// <param name="token">地图退出时取消任务会话的令牌。</param>
        /// <returns>地图任务会话完成加载后的异步任务。</returns>
        public UniTask OnMapEnteredAsync(int mapId, CancellationToken token)
        {
            return QuestView.Instance.OnMapEnteredAsync(mapId, token);
        }

        /// <summary>地图离开时关闭任务 HUD 并清理地图任务会话。</summary>
        public void CloseMapUI()
        {
            QuestView.Instance.CloseMapUI();
        }

        /// <summary>读取地图任务物件的本地化交互提示。</summary>
        public string TaskInteractionPrompt => GF.Localization.GetText("Quest.UI.Interact");

        /// <summary>读取地图 NPC 的本地化交谈提示。</summary>
        public string NpcDialoguePrompt => GF.Localization.GetText("Dialogue.UI.Interact");

        /// <summary>播放对话并将 UI 层结果转换为 Bridge 结果。</summary>
        /// <param name="dialogueId">对话配置编号。</param>
        /// <param name="token">取消对话的令牌。</param>
        /// <returns>不暴露 UI 模块类型的对话结果。</returns>
        public async UniTask<DialoguePlaybackResult> PlayDialogueAsync(int dialogueId, CancellationToken token)
        {
            DialogueResult result = await DialogueView.Instance.PlayAsync(dialogueId, token);
            return new DialoguePlaybackResult((DialoguePlaybackStatus)result.Status, result.Choices);
        }

        /// <summary>读取当前认证状态，供 Gameplay 决定是否开放 NPC 交互。</summary>
        public bool IsPlayerAuthenticated => LoginModel.Instance.IsAuthenticated;

        /// <summary>请求 Login 模块启动当前认证会话的重连。</summary>
        public void BeginReconnect()
        {
            LoginController.Instance.BeginReconnect();
        }

        /// <summary>玩法结束后恢复登录界面并显示断线原因。</summary>
        /// <param name="message">向玩家显示的退出原因。</param>
        /// <returns>登录界面恢复完成后的异步任务。</returns>
        public UniTask ReturnToLoginAsync(string message)
        {
            return LoginController.Instance.ReturnToLoginAsync(message);
        }

        /// <summary>UI 可接收的角色战斗 HUD 数据，不包含玩法对象引用。</summary>
        public readonly struct RoleHudSnapshot
        {
            /// <summary>角色实例编号。</summary>
            public int RoleId { get; }
            /// <summary>角色配置的操作编号。</summary>
            public int OperateId { get; }
            /// <summary>当前生命值。</summary>
            public float Hp { get; }
            /// <summary>最大生命值。</summary>
            public float MaxHp { get; }

            internal RoleHudSnapshot(int roleId, int operateId, float hp, float maxHp)
            {
                RoleId = roleId;
                OperateId = operateId;
                Hp = hp;
                MaxHp = maxHp;
            }
        }

        /// <summary>对话播放结果状态。</summary>
        public enum DialoguePlaybackStatus
        {
            /// <summary>对话正常完成。</summary>
            Completed,
            /// <summary>对话被取消。</summary>
            Cancelled,
            /// <summary>对话播放失败。</summary>
            Failed,
            /// <summary>已有另一个对话会话。</summary>
            Busy
        }

        /// <summary>Gameplay 可消费的对话播放结果。</summary>
        public readonly struct DialoguePlaybackResult
        {
            /// <summary>此次对话的终态。</summary>
            public DialoguePlaybackStatus Status { get; }
            /// <summary>按选择顺序记录的选项编号。</summary>
            public IReadOnlyList<int> Choices { get; }

            internal DialoguePlaybackResult(DialoguePlaybackStatus status, IReadOnlyList<int> choices)
            {
                Status = status;
                Choices = choices;
            }
        }

        private static RoleHudSnapshot CreateSnapshot(RoleUnit roleUnit)
        {
            RoleData data = roleUnit.Data;
            return new RoleHudSnapshot(roleUnit.RoleId, data.OperateId, (float)data.Resource.hp, (float)data.Prop.Hp);
        }
    }
}
