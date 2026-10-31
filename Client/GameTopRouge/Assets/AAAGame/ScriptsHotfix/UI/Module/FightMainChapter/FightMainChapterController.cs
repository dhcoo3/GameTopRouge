using AAAGame.ScriptsHotfix.Bridge;
using AAAGame.ScriptsHotfix.UI.Core;
using AAAGame.ScriptsHotfix.UI.Module.FightCommon;
using Cysharp.Threading.Tasks;
using cfg.UI;

namespace AAAGame.ScriptsHotfix.UI.Module.FightMainChapter
{
    #if ENABLE_OBFUZ
    [Obfuz.ObfuzIgnore]
    #endif
    public class FightMainChapterController : ModuleController<FightMainChapterController>,IController
    {
        public override void RegisterProto()
        {
     
        }

        public override void RegisterEvent()
        {
        
        }

        /// <summary>
        /// 启动主战斗逻辑并等待主战斗表单打开。
        /// </summary>
        /// <returns>主战斗表单打开完成的异步任务。</returns>
        public async UniTask EnterFightAsync()
        {
           UIToGamePlayerBridge.Instance.GameStart();
           // UIToGamePlayerBridge.Instance.GameStartByRoom(GF.Room.RoomInfo);
            await UIModuleManager.Instance.GoToModuleAsync(ModuleType.FightMainChapter);
        }

        public void ExitFight()
        {

        }

        public void InitFightFSM()
        {
           
        }
    }
}
