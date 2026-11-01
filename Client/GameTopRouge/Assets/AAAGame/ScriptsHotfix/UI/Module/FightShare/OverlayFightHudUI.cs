using AAAGame.ScriptsHotfix.Bridge;
using AAAGame.ScriptsHotfix.UI.Module.FightCommon;
using UnityEngine;

namespace AAAGame.ScriptsHotfix.UI.Module.FightShare
{
    #if ENABLE_OBFUZ
    [Obfuz.ObfuzIgnore]
    #endif
    public class OverlayFightHudUI : UGF_Panel
    {
        #region Auto Create
        #endregion
        
        protected override void RegisterUI()
        {
            #region Auto Bind
            #endregion
        }

        private GameObject RoleHudPrefab;
        private FightShareModel _fighShareModel;
        
        protected override void RegisterEvent()
        {
            Subscribe(FightShareConst.Event.EUpdatePos,EUpdatePosHandler);
            Subscribe(FightShareConst.Event.EUpdateHp,EUpdateHpHandler);
            Subscribe(FightShareConst.Event.ERoleDie,ERoleDieHandler);
        }

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
        }

        protected override void OnOpen(object userData)
        {
            _fighShareModel = FightShareModel.Instance;
            LoadRoleHudUI();
            base.OnOpen(userData);
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            GF.AssetBridge.UnloadAsset(RoleHudPrefab);
            base.OnClose(isShutdown, userData);
        }

        private void LoadRoleHudUI()
        {
            if (RoleHudPrefab == null)
            {
                string roleHudPath = GF.AssetBridge.GetUIFormAsset("FightCommon/RoleHud");
                GF.AssetBridge.LoadAsset(roleHudPath,LoadRoleHudUIHandler,typeof(GameObject));
            }
        }

        private void LoadRoleHudUIHandler(AssetInfo obj)
        {
            RoleHudPrefab = obj.Asset as GameObject;
        }
        
        private void EUpdateHpHandler(object sender, GameEvent e)
        {
            GamePlayToUIBridge.RoleHudSnapshot roleData = e.GetParam1<GamePlayToUIBridge.RoleHudSnapshot>();
            if (_fighShareModel.FightUiRoles.TryGetValue(roleData.RoleId, out FightUIRole fightUIRole))
            {
                if (fightUIRole.Hud == null)
                {
                    fightUIRole.Hud = new FightUIRoleHud(fightUIRole);
                    fightUIRole.Hud.RegisterUI(GameObject.Instantiate(RoleHudPrefab,transform));
                }

                fightUIRole.Hud.UpdateHudHp(roleData);
            }
        }

        private void EUpdatePosHandler(object sender, GameEvent e)
        {
            int id = e.GetParam1<int>();
            Vector3 pos = e.GetParam2<Vector3>();
            if (_fighShareModel.FightUiRoles.TryGetValue(id, out FightUIRole fightUIRole))
            {
                if (fightUIRole.Hud == null)
                {
                    fightUIRole.Hud = new FightUIRoleHud(fightUIRole);
                    fightUIRole.Hud.RegisterUI(GameObject.Instantiate(RoleHudPrefab,transform));
                }

                Vector2 uiPos = Utils.WorldToUIPoint(pos);
                fightUIRole.Hud.UpdateHudPos(uiPos.x, uiPos.y + 100);
            }
        }
        
        private void ERoleDieHandler(object sender, GameEvent e)
        {
            GamePlayToUIBridge.RoleHudSnapshot snapshot = e.GetParam1<GamePlayToUIBridge.RoleHudSnapshot>();
            if (_fighShareModel.FightUiRoles.TryGetValue(snapshot.RoleId, out FightUIRole fightUIRole) && fightUIRole.Hud != null)
                fightUIRole.Hud.UpdateHudHp(snapshot);
        }
    }
}
