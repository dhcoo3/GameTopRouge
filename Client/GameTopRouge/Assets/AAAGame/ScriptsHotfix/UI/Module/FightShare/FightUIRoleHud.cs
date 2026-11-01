using AAAGame.ScriptsHotfix.Bridge;
using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.UI.Module.FightCommon
{
    public class FightUIRoleHud
    {
        private UIContainer m_uiNode;
        
        private UGF_Image m_HpBarBg;
        private UGF_Image m_HpBar;
        private UGF_Text m_Name;
  
        
        private Vector2 _tmpV2 = Vector2.zero;
        private RectTransform _rectTransform;
        private FightUIRole _fightUIRole;
        
        public FightUIRoleHud(FightUIRole fightUIRole)
        {
            _fightUIRole = fightUIRole;
        }
        
        public void RegisterUI(GameObject go)
        {
            m_uiNode = go.GetComponent<UIContainer>();
            
            if (m_uiNode == null)
            {
                return;
            }
            
            m_HpBarBg = m_uiNode.Get<UGF_Image>("m_HpBarBg");
            m_HpBar = m_uiNode.Get<UGF_Image>("m_HpBar");
            m_Name = m_uiNode.Get<UGF_Text>("m_Name");
            
            _rectTransform = m_uiNode.GetComponent<RectTransform>();
            
            m_Name.SetText(_fightUIRole.Snapshot.OperateId.ToString());
        }

        public void UpdateHudPos(float x, float y)
        {
            _tmpV2.x = x;
            _tmpV2.y = y;
            _rectTransform.anchoredPosition =_tmpV2;
        }
        
        public void UpdateHudHp(GamePlayToUIBridge.RoleHudSnapshot snapshot)
        {
            float hpRatio = snapshot.MaxHp > 0 ? snapshot.Hp / snapshot.MaxHp : 0;
            m_HpBar.DoFillAmount(hpRatio,0.1f,(f =>
            {
                
            } ));
        }
    }
}
