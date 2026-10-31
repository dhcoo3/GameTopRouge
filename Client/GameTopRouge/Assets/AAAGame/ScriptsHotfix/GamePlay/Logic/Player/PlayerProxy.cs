using System.Collections.Generic;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Common;
using AAAGame.ScriptsHotfix.GamePlay.Logic.ProxyManager;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Player
{
    public class PlayerProxy:GameProxy
    {
        private List<PlayerData> _players = new List<PlayerData>(5);

        public List<PlayerData> Players
        {
            get { return _players; }
        }
        
        public override void Initialize()
        {
           
        }

        public void IniPlayers(List<PlayerData> players)
        {
            foreach (var data in players)
            {
                AddPlayer(data);
            }
        }

        public void AddPlayer(PlayerData player)
        {
            _players.Add(player);
        }
    }
}