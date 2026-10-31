using System.Collections.Generic;
using GameFramework;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Player
{
    public class PlayerData:IReference
    {
        public int Id {get;private set;}
        public string Name{get;private set;}

        public int Pos = 0;
        
        public List<int> HeroIds;

        public static PlayerData Create(int id, string name)
        {
            PlayerData playerData = new PlayerData();
            playerData.Id = id;
            playerData.Name = name;
            return playerData;
        }
        
        public void Clear()
        {
            Id = 0;
            Name = string.Empty;
            Pos = 0;
        }
    }
}