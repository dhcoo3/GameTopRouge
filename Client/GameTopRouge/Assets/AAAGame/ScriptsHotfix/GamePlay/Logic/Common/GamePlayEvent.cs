using System;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Common
{
    public class GamePlayEvent
    {
        public static readonly Int16 ECreateRoleAll = ++GameEvent.UIEventId;
        public static readonly Int16 ERenderAllRole = ++GameEvent.UIEventId;
        public static readonly Int16 ECreateMapRender = ++GameEvent.UIEventId;
        public static readonly Int16 EFightLoadingFinish = ++GameEvent.UIEventId;
        public static readonly Int16 ECameraFocus = ++GameEvent.UIEventId;
        public static readonly Int16 ERenderBullet = ++GameEvent.UIEventId;
        public static readonly Int16 EStopRenderBullet = ++GameEvent.UIEventId;
        public static readonly Int16 ERenderMonster = ++GameEvent.UIEventId;
        public static readonly Int16 EStopRenderRole = ++GameEvent.UIEventId;
        public static readonly Int16 EPlayEffect = ++GameEvent.UIEventId;
        public static readonly Int16 ERenderAoe = ++GameEvent.UIEventId;
        public static readonly Int16 EStopRenderAoe = ++GameEvent.UIEventId;
        
        public static readonly Int16 EUpdateFrame = ++GameEvent.UIEventId;
        public static readonly Int16 ESvrRoomPlayerInfoUpdate = ++GameEvent.UIEventId;
    }
}