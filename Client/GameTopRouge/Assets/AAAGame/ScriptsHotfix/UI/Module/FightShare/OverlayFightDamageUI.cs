using System.Collections;
using UnityEngine;

namespace AAAGame.ScriptsHotfix.UI.Module.FightShare
{
    public class OverlayFightDamageUI:UGF_Panel
    {
        #region Auto Create
        #endregion
        
        private GameObject DamageTextUI;
        
        protected override void RegisterUI()
        {
            #region Auto Bind
            #endregion
        }
        
        protected override void RegisterEvent()
        {
            Subscribe(FightShareConst.Event.EPopUpNumber,EPopUpNumberHandler);
        }

        protected override void OnOpen(object userData)
        {
            LoadDamageText();
            base.OnOpen(userData);
        }
        
        private void LoadDamageText()
        {
            if (DamageTextUI == null)
            {
                string roleHudPath = GF.AssetBridge.GetUIFormAsset("FightCommon/DamageTextUI");
                GF.AssetBridge.LoadAsset(roleHudPath,LoadDamageTextUIHandler,typeof(GameObject));
            }
        }

        private void LoadDamageTextUIHandler(AssetInfo obj)
        {
            DamageTextUI = obj.Asset as GameObject;
        }
        
        private void EPopUpNumberHandler(object sender, GameEvent e)
        {
            Vector3 pos = e.GetParam1<Vector3>();
            int damageVal = e.GetParam2<int>();
            bool isHeal = e.GetParam3<bool>();
            StartCoroutine(StartShow(pos,damageVal,isHeal));
        }
        
        IEnumerator StartShow(Vector3 pos,int damageVal,bool isHeal)
        {
            // 调用异步实例化方法
            var instantiateOperation = UnityEngine.Object.InstantiateAsync(DamageTextUI,this.transform);
        
            // 等待实例化完成
            yield return instantiateOperation;
        
            // 获取实例化后的对象
            GameObject[] instantiatedObj = instantiateOperation.Result;
        
            if (instantiatedObj != null)
            {
                UIContainer uiContainer = instantiatedObj[0].GetComponent<UIContainer>();
                uiContainer.Get<UGF_Text>("m_Damage").SetText(damageVal.ToString());
                
                Vector2 uiPos = Utils.WorldToUIPoint(pos);
                uiPos.y += 120;
                
                // 对实例化对象进行操作，例如设置位置
                instantiatedObj[0].GetComponent<RectTransform>().anchoredPosition = uiPos;
                
                Debug.Log("异步实例化成功！");
            }

            yield return new WaitForSeconds(2f);
            
            if (instantiatedObj != null)
            {
                GameObject.Destroy(instantiatedObj[0]);
            }
        }
    }
}
