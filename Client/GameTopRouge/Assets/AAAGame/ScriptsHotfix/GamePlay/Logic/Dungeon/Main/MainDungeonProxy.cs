using System;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Map;
using AAAGame.ScriptsHotfix.GamePlay.Logic.ProxyManager;
using cfg.Chapter;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Dungeon.Main
{
    public class MainDungeonProxy:GameProxy,IDungeon
    {
        private MapProxy _mapProxy;
        private TbMainChapter _tbMainChapter;
        
        public override async void Initialize()
        {
            try
            {
                _tbMainChapter = await GF.LuBan.Get<TbMainChapter>(cfg.Tables.chapter_tbmainchapter);
            }
            catch (Exception e)
            {
                Log.Error("Initialize error = {0}",e.Message);
            }
            
            _mapProxy = GetProxy<MapProxy>();
        }

        public void InitDungeon(int dungeonID)
        {
            MainChapter chapterData = _tbMainChapter.GetOrDefault(dungeonID);
            if (chapterData == null)
            {
                Log.Error("不存在主线关卡数据 {0}",dungeonID);
                return;
            }
            _mapProxy.LoadMap(chapterData.MapId);
        }

        public override void Clear()
        {            
            base.Clear();
        }
    }
}