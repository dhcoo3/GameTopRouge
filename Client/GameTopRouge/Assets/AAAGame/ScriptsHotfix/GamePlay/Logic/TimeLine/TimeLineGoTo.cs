namespace AAAGame.ScriptsHotfix.GamePlay.Logic.TimeLine
{
    public struct TimeLineGoTo
    {
        ///<summary>
        ///自身处于时间点
        ///</summary>
        public fix atDuration;

        ///<summary>
        ///跳转到时间点
        ///</summary>
        public fix gotoDuration;

        public TimeLineGoTo(fix atDuration, fix gotoDuration){
            this.atDuration = atDuration;
            this.gotoDuration = gotoDuration;
        }

        public static TimeLineGoTo Null = new TimeLineGoTo(fix.MaxValue, fix.MaxValue);
    }
}