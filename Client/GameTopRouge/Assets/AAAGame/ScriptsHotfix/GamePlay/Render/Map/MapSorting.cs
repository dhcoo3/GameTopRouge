using UnityEngine;

namespace AAAGame.ScriptsHotfix.GamePlay.Render.Map
{
    internal static class MapSorting
    {
        public const int WorldObjectBase = 1000;
        public const int WorldObjectYScale = 100;

        public static int WorldObjectOrder(float footY)
        {
            return WorldObjectBase - Mathf.RoundToInt(footY * WorldObjectYScale);
        }
    }
}
