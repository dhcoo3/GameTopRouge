namespace AAAGame.ScriptsHotfix.GamePlay.Render.Map
{
    /// <summary>地图中各类表现对象使用的 Sprite 排序层级。</summary>
    public enum MapRenderLayer
    {
        /// <summary>地图地面和网格。</summary>
        Grid = 0,
        /// <summary>玩家角色和普通场景摆放对象。</summary>
        Role = 1,
        /// <summary>地图特效和传送门。</summary>
        Effect = 3000,
        /// <summary>子弹表现。</summary>
        Bullet = 4000,
        /// <summary>范围效果表现。</summary>
        Aoe = 5000,
    }
}
