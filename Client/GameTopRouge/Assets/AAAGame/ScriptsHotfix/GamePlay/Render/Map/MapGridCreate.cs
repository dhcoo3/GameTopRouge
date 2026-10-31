using UnityEngine;

namespace AAAGame.ScriptsHotfix.GamePlay.Render.Map
{
    public class MapGridCreate
    {
        public GridInfo GridInfo;
        public int Row;
        public int Col;
        public fix Rotation;
        public fix3 BirthPosition;

        public MapGridCreate(GridInfo gridInfo, int row,int col,fix3 position,fix rotation)
        {
            this.GridInfo = gridInfo;
            this.Row = row;
            this.Col = col;
            this.BirthPosition = position;
            this.Rotation = rotation;
        }
    }
}