using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AAAGame.ScriptsHotfix.GamePlay;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Player;
using AAAGame.ScriptsHotfix.UI.Module.Login;
using Fantasy;
using Fantasy.Network;
using UnityGameFramework.Runtime;
using Log = UnityGameFramework.Runtime.Log;

namespace AAAGame.ScriptsHotfix.Bridge
{
    /// <summary>
    /// UI向战斗层通信
    /// </summary>
    public class UIToGamePlayerBridge:Singleton<UIToGamePlayerBridge>
    {
        /// <summary>
        /// 单人模式
        /// </summary>
        public void GameStart()
        {
            if (!LoginModel.Instance.IsAuthenticated || GF.UserId <= 0)
                throw new System.InvalidOperationException("进入主线前必须完成游戏服认证。");
            GF.UI.CloseAllLoadingUIForms();
            GF.UI.CloseAllLoadedUIForms();
            List<PlayerData> list = new List<PlayerData>();
            PlayerData playerData = PlayerData.Create(GF.UserId, LoginModel.Instance.PlayerName);
            playerData.HeroIds = new List<int>(){1000001};
            list.Add(playerData);
            
            GamePlayFacade.Instance.Run(false,list,1,cfg.Fight.DungeonType.Main);
        }
        
        /// <summary>
        /// 创建一间默认主线副本的双人帧同步房间，供界面或联调入口调用。
        /// </summary>
        /// <param name="heroId">创建者选择的英雄配置标识。</param>
        /// <returns>成功时返回房间标识，失败时返回零。</returns>
        public async Task<int> CreateFrameRoomAsync(int heroId)
        {
            if (!LoginModel.Instance.IsAuthenticated) return 0;
            Session session = GF.GameNetwork.Session;
            if (session == null || session.IsDisposed)
            {
                LoginController.Instance.BeginReconnect();
                return 0;
            }
            try
            {
                using var response = await session.C2G_CreateFrameRoom(1, (int)cfg.Fight.DungeonType.Main, heroId);
                if (response.ErrorCode == 0) return response.RoomId;
                Log.Warning("创建帧同步房间失败：{0}", response.ErrorMessage);
            }
            catch (Exception)
            {
                LoginController.Instance.BeginReconnect();
            }
            return 0;
        }

        /// <summary>
        /// 加入指定双人帧同步房间，第二名成员加入后服务端会自动下发开局描述。
        /// </summary>
        /// <param name="roomId">需要加入的房间标识。</param>
        /// <param name="heroId">加入者选择的英雄配置标识。</param>
        /// <returns>服务端确认加入成功时返回 true。</returns>
        public async Task<bool> JoinFrameRoomAsync(int roomId, int heroId)
        {
            if (!LoginModel.Instance.IsAuthenticated) return false;
            Session session = GF.GameNetwork.Session;
            if (session == null || session.IsDisposed)
            {
                LoginController.Instance.BeginReconnect();
                return false;
            }
            try
            {
                using var response = await session.C2G_JoinFrameRoom(roomId, heroId);
                if (response.ErrorCode == 0) return true;
                Log.Warning("加入帧同步房间失败：{0}", response.ErrorMessage);
            }
            catch (Exception)
            {
                LoginController.Instance.BeginReconnect();
            }
            return false;
        }

        /*/// <summary>
        /// 房间模式
        /// </summary>
        /// <param name="room"></param>
        public void GameStartByRoom(RelayRoom room)
        {
            GF.UI.CloseAllLoadingUIForms();
            GF.UI.CloseAllLoadedUIForms();

            List<PlayerData> list = new List<PlayerData>();
            foreach (var (key,data) in room.Players)
            {
                PlayerData playerData = PlayerData.Create(data.ID, data.Name);
                playerData.HeroIds = new List<int>(){1000001};
                playerData.Pos = (int)data.TransportId;
                list.Add(playerData);
            }
            
            GamePlayFacade.Instance.Run(true,list,1,cfg.Fight.DungeonType.Main);
        }*/
    }
}
