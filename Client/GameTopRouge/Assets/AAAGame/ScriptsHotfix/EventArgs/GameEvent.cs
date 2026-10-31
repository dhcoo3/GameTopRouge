using System;
using GameFramework;
using GameFramework.Event;
using UnityGameFramework.Runtime;

public class GameEvent : GameEventArgs
{
    public static Int16 UIEventId = 1;
    
    private Int16 m_EventId = 0;

    private object[] m_Args;

    public object[] Args
    {
        get { return m_Args; }
    }
    
    public static GameEvent Create(Int16 id, params object[] args)
    {
        GameEvent gameEvent = ReferencePool.Acquire<GameEvent>();
        gameEvent.m_EventId = id;
        if (args != null)
        {
            gameEvent.m_Args = args;
        }
        return gameEvent;
    }
    
    public override void Clear()
    {
        m_Args = null;
        m_EventId = 0;
    }
    
    /// <summary>
    /// 获取加载字典成功事件编号。
    /// </summary>
    public override int Id
    {
        get
        {
            return m_EventId;
        }
    }

    public T GetParam1<T>()
    {
        try
        {
            return (T)m_Args[0];
        }
        catch (Exception e)
        {
            Log.Error(string.Format("GetParam1 error:{0} {1} {2}", e.Message,m_Args[0],typeof(T)));
        }
        
        return default(T);
    }
    
    public T GetParam2<T>()
    {
        try
        {
            return (T)m_Args[1];
        }
        catch (Exception e)
        {
            Log.Error(string.Format("GetParam2 error:{0} {1} {2}", e.Message,m_Args[1],typeof(T)));
        }
        
        return default(T);
    }
    
    public T GetParam3<T>()
    {
        try
        {
            return (T)m_Args[2];
        }
        catch (Exception e)
        {
            Log.Error(string.Format("GetParam3 error:{0} {1} {2}", e.Message,m_Args[2],typeof(T)));
        }
        
        return default(T);
    }
}
