
using cfg.UI;

public class RedDotTask:IRedDotStrategy
{
    public RedDotType Id
    {
        get
        {
            return RedDotType.Task;
        }
    }

    public bool ConditionFunc()
    {
        return false;
    }

    public int CountFunc()
    {
        return 0;
    }
}
