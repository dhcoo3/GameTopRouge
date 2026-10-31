using AAAGame.ScriptsHotfix.GamePlay.Logic.Role;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Role;
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
            
            m_Name.SetText(_fightUIRole.RoleUnit.Data.OperateId.ToString());
        }

        public void UpdateHudPos(float x, float y)
        {
            _tmpV2.x = x;
            _tmpV2.y = y;
            _rectTransform.anchoredPosition =_tmpV2;
        }
        
        public void UpdateHudHp(RoleUnit roleUnit)
        {
            m_HpBar.DoFillAmount(roleUnit.Data.Resource.hp/(float)roleUnit.Data.Prop.Hp,0.1f,(f =>
            {
                
            } ));
        }
    }
}