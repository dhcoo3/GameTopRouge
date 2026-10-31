
using cfg.UI;

public interface IRedDotStrategy
{
    abstract RedDotType Id {get;}

    /// <summary>
    /// 实现红点是否显示逻辑
    /// </summary>
    abstract bool ConditionFunc();
    
    /// <summary>
    /// 实现红点计数逻辑
    /// </summary>
    abstract int CountFunc();
}
