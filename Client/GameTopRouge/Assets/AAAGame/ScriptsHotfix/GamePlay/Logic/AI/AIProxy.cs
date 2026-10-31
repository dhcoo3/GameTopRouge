using System;
using System.Collections.Generic;
using AAAGame.ScriptsHotfix.GamePlay.Logic.ProxyManager;
using cfg.AI;
using GameFramework;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.AI
{
    /// <summary>
    /// 管理正在运行的AI，可能多个角色共用一个AI片段
    /// </summary>
    public class AIProxy:GameProxy
    {
        private TbAIGroupDef _tbAIGroupDef;
        private TbAIActionDef _tbAIActionDef;
        
        /// <summary>
        /// AI片段，可复用
        /// </summary>
        Dictionary<int,AIClip> _aiClips = new Dictionary<int,AIClip>();
        
        public override async void Initialize()
        {
            try
            {
                _tbAIGroupDef = await GF.LuBan.Get<TbAIGroupDef>(cfg.Tables.ai_tbaigroupdef);
                _tbAIActionDef =  await GF.LuBan.Get<TbAIActionDef>(cfg.Tables.ai_tbaiactiondef);
            }
            catch (Exception e)
            {
                Log.Error("Initialize error = {0}",e.Message);
            }
        }

        public override void Clear()
        {
            foreach (AIClip clip in _aiClips.Values)
            {
                ReferencePool.Release(clip);
            }
            
            _aiClips.Clear();
            base.Clear();
        }

        public List<AIClip> GetAIClips(int id)
        {
            List<AIClip> aiClips = new List<AIClip>();
            
            AIGroupDef aiGroupDef = _tbAIGroupDef.GetOrDefault(id);

            if (aiGroupDef == null)
            {
                Log.Warning("aiGroupDef is null {0}",id);
                return aiClips;
            }

            for (int i = 0; i < aiGroupDef.AiActionList.Count; i++)
            {
                int aiActionId = aiGroupDef.AiActionList[i];

                if (_aiClips.TryGetValue(aiActionId, out AIClip aiClip))
                {
                    aiClips.Add(aiClip);
                    continue;
                }
                
                AIActionDef aiActionDef = _tbAIActionDef.GetOrDefault(aiActionId);

                if (aiActionDef == null)
                {
                    Log.Warning("aiActionDef is null {0}",aiActionId);
                    continue;
                }

                aiClips.Add(AIClip.Create(aiActionDef));
            }

            return aiClips;
        }
    }
}