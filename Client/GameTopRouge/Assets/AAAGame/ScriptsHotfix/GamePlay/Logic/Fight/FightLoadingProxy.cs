using AAAGame.ScriptsHotfix.GamePlay.Logic.Common;
using AAAGame.ScriptsHotfix.GamePlay.Logic.ProxyManager;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Fight
{
    public class FightLoadingProxy:GameProxy
    {
        private float _roleProgress;
        private float _sceneProgress;
        private int _prefabMaxProgress;

        private bool _isLoading;

        public override void Initialize()
        {
            _isLoading = true;
            _roleProgress = 0;
            _sceneProgress = 0;
        }

        public override void Clear()
        {
            _isLoading = true;
            _roleProgress = 0;
            _sceneProgress = 0;
            base.Clear();
        }
        
        public void SetRoleProgress(float currentProgress)
        {
            _roleProgress = currentProgress;
            CheckLoadFinished();
        }
        
        public void SetSceneProgress(float currentProgress)
        {
            _sceneProgress = currentProgress;
            CheckLoadFinished();
        }

        private void CheckLoadFinished()
        {
            if(_roleProgress < 1 || _sceneProgress < 1) return;
            if(!_isLoading) return;
            _isLoading = false;
            Fire(GamePlayEvent.EFightLoadingFinish);
        }
    }
}