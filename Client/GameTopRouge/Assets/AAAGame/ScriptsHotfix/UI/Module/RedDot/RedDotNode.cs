using System;
using System.Collections.Generic;
using System.Linq;
using cfg.UI;

/// <summary>
/// 红点节点类
/// </summary>
public class RedDotNode
{
    /// <summary>
    /// 红点类型
    /// </summary>
    public RedDotType Type { get; private set; }
    
    /// <summary>
    /// 父节点
    /// </summary>
    public RedDotNode Parent { get; private set; }
    
    /// <summary>
    /// 子节点字典
    /// </summary>
    private Dictionary<RedDotType, RedDotNode> _children = new Dictionary<RedDotType, RedDotNode>();
    
    /// <summary>
    /// 红点显示状态
    /// </summary>
    public bool IsActive { get; private set; }
    
    /// <summary>
    /// 红点数量
    /// </summary>
    public int Count { get; private set; }
    
    /// <summary>
    /// 红点状态变化回调
    /// </summary>
    public event Action<RedDotNode> OnStateChanged;
    
    /// <summary>
    /// 红点条件判断委托
    /// </summary>
    private Func<bool> _condition;
    
    /// <summary>
    /// 红点数量计算委托
    /// </summary>
    private Func<int> _countFunc;

    public RedDotNode(RedDotType type, RedDotNode parent = null)
    {
        Type = type;
        Parent = parent;
    }

    /// <summary>
    /// 添加子节点
    /// </summary>
    public void AddChild(RedDotNode child)
    {
        if (child == null || _children.ContainsKey(child.Type))
        {
            return;
        }
        
        _children.Add(child.Type, child);
        child.Parent = this;
    }

    /// <summary>
    /// 获取子节点
    /// </summary>
    public RedDotNode GetChild(RedDotType type)
    {
        _children.TryGetValue(type, out var child);
        return child;
    }

    /// <summary>
    /// 移除子节点
    /// </summary>
    public void RemoveChild(RedDotType type)
    {
        if (_children.ContainsKey(type))
        {
            _children.Remove(type);
        }
    }

    /// <summary>
    /// 设置条件判断
    /// </summary>
    public void SetCondition(Func<bool> condition)
    {
        _condition = condition;
    }

    /// <summary>
    /// 设置数量计算函数
    /// </summary>
    public void SetCountFunc(Func<int> countFunc)
    {
        _countFunc = countFunc;
    }

    /// <summary>
    /// 更新红点状态
    /// </summary>
    public void UpdateState()
    {
        bool oldState = IsActive;
        int oldCount = Count;

        if (_children.Count > 0)
        {
            //是否存在至少一个子节点的 IsActive 属性为 true
            IsActive = _children.Values.Any(child => child.IsActive);

            if (!IsActive)
            {
                // 如果有条件判断函数，则使用它
                if (_condition != null)
                {
                    IsActive = _condition.Invoke();
                }
            }
        }
        else
        {
            // 如果有条件判断函数，则使用它
            if (_condition != null)
            {
                IsActive = _condition.Invoke();
            }
        }
        
        // 更新数量
        if (_countFunc != null)
        {
            Count = _countFunc.Invoke();
            // 如果有数量，则状态为true
            if (Count > 0)
            {
                IsActive = true;
            }
        }
        
        // 如果状态或数量发生变化，触发回调
        if (oldState != IsActive || oldCount != Count)
        {
            OnStateChanged?.Invoke(this);
            
            // 通知父节点更新
            Parent?.UpdateState();
        }
    }

    /// <summary>
    /// 强制设置红点状态（慎用，通常应该通过条件自动更新）
    /// </summary>
    public void SetActive(bool active)
    {
        if (IsActive != active)
        {
            IsActive = active;
            OnStateChanged?.Invoke(this);
            Parent?.UpdateState();
        }
    }

    /// <summary>
    /// 强制设置红点数量（慎用，通常应该通过计数函数自动更新）
    /// </summary>
    public void SetCount(int count)
    {
        if (Count != count)
        {
            Count = count;
            IsActive = count > 0;
            OnStateChanged?.Invoke(this);
            Parent?.UpdateState();
        }
    }
}