namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Frame
{
    /// <summary>
    /// 定义帧同步协议中由客户端玩法层使用的固定字段值。
    /// </summary>
    internal static class FrameCommandDefine
    {
        /// <summary>
        /// 客户端每个确定性逻辑步的秒数，对应目标 30Hz。
        /// </summary>
        public const float LogicFrameRate = 1f / 30f;
        
        /// <summary>
        /// 服务端逻辑帧的起始编号。
        /// </summary>
        public const int FirstFrameId = 1;

        /// <summary>
        /// 移动命令的 CommandType 值。
        /// </summary>
        public const int MoveCommandType = 1;

        /// <summary>
        /// 技能命令的 CommandType 值。
        /// </summary>
        public const int SkillCommandType = 2;

        /// <summary>
        /// 协议中归一化移动方向的整数缩放倍率。
        /// </summary>
        public const int CoordinateScale = 10000;
    }
}
