using AAAGame.ScriptsHotfix.GamePlay.Logic.Common;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Map;
using AAAGame.ScriptsHotfix.GamePlay.Logic.ProxyManager;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Role;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.MonsterSpawner
{
    public class MonsterSpawnerProxy:GameProxy
    {
        private bool StartCreate;
        
        private int MaxCount = 1;
        
        private UnitProxy _unitProxy;

        private MapProxy _mapProxy;
        public override void Initialize()
        {
            _unitProxy = GetProxy<UnitProxy>();
            _mapProxy = GetProxy<MapProxy>();
            Subscribe(GamePlayEvent.EFightLoadingFinish,FightLoadingFinish);
        }

        public override void Clear()
        {
            StartCreate = false;
            base.Clear();
        }

        public override void LogicUpdate(fix deltaTime)
        {
            if(!StartCreate) return;
            
            if (GetCount() < MaxCount)
            {
                int createCount = MaxCount - GetCount();
                for (int i = 0; i < createCount; i++)
                {
                    CreateMonster();
                }
            }
        }

        private void CreateMonster()
        {
            RoleUnit roleUnit = _unitProxy.CreateMonsterRoleUnit(1100001);
            Fire(GamePlayEvent.ERenderMonster,roleUnit);
        }

        private int GetCount()
        {
            return _unitProxy.Monsters.Count;
        }
        
        private void FightLoadingFinish(object sender, GameEvent e)
        {
            StartCreate = true;
        }
    }
}