using System;

namespace AAAGame.ScriptsHotfix.UI.Module.FightShare
{
    #if ENABLE_OBFUZ
    [Obfuz.ObfuzIgnore]
    #endif
    /// <summary>
    /// 该模块常量定义
    /// </summary>
    public class FightShareConst
    {
        public const string OverlayFightHudUI = "OverlayFightHudUI";
        public const string OverlayFightDamageUI = "OverlayFightDamageUI";
        public class Event
        {
            public static readonly Int16 ECreateRole = ++GameEvent.UIEventId;
            public static readonly Int16 EUpdatePos = ++GameEvent.UIEventId;
            public static readonly Int16 EUpdateHp = ++GameEvent.UIEventId;
            public static readonly Int16 ERoleDie = ++GameEvent.UIEventId;
            public static readonly Int16 EPopUpNumber = ++GameEvent.UIEventId;
        }
    }
}
