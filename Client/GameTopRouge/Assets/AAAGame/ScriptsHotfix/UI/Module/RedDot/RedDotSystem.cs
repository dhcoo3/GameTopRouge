using System;
using System.Collections.Generic;
using cfg.UI;
using UnityGameFramework.Runtime;

/// <summary>
/// 红点系统管理器
/// </summary>
public class RedDotSystem: Singleton<RedDotSystem>
{
    /// <summary>
    /// 根节点
    /// </summary>
    private RedDotNode _root;

    /// <summary>
    /// 所有节点字典
    /// </summary>
    private readonly Dictionary<RedDotType, RedDotNode> _allNodes = new Dictionary<RedDotType, RedDotNode>();
    
    /// <summary>
    /// 红点处理方法
    /// </summary>
    private readonly Dictionary<RedDotType, IRedDotStrategy> _strategies = new Dictionary<RedDotType, IRedDotStrategy>();

    private TbRedDot _TbRedDot;
    
    public RedDotSystem()
    {
        // 初始化红点树结构
        InitializeRedDotTree();
    }

    /// <summary>
    /// 初始化红点树结构
    /// </summary>
    private async void InitializeRedDotTree()
    {
        try
        {
            _TbRedDot = await GF.LuBan.Get<TbRedDot>(cfg.Tables.ui_tbreddot);
        
            foreach (RedDot data in _TbRedDot.DataList)
            {
                // 创建各个主要节点
                var taskNode = new RedDotNode(data.Id,_root);
                RegisterNode(taskNode);
            
                if (data.Child.Count > 0)
                {
                    foreach (var childType in data.Child)
                    {
                        var childNode = new RedDotNode(childType);
                        taskNode.AddChild(childNode);
                        RegisterStrategy(childNode);
                        RegisterNode(childNode);
                    }   
                }
            }
        }
        catch (Exception e)
        {
            Log.Error("InitializeRedDotTree Error {0}",e.Message);
        }
    }

    /// <summary>
    /// 注册节点到字典
    /// </summary>
    private void RegisterNode(RedDotNode node)
    {
        if (node == null || _allNodes.ContainsKey(node.Type))
        {
            return;
        }
        
        _allNodes.Add(node.Type, node);
    }

    
    /// <summary>
    /// 获取红点节点
    /// </summary>
    public RedDotNode GetNode(RedDotType type)
    {
        _allNodes.TryGetValue(type, out var node);
        return node;
    }

    /// <summary>
    /// 设置红点条件
    /// </summary>
    public void SetCondition(RedDotType type, Func<bool> condition)
    {
        var node = GetNode(type);
        if (node != null)
        {
            node.SetCondition(condition);
        }
    }

    /// <summary>
    /// 设置红点数量计算函数
    /// </summary>
    public void SetCountFunc(RedDotType type, Func<int> countFunc)
    {
        var node = GetNode(type);
        if (node != null)
        {
            node.SetCountFunc(countFunc);
        }
    }

    /// <summary>
    /// 更新所有红点状态
    /// </summary>
    public void UpdateAll()
    {
        foreach (var node in _allNodes.Values)
        {
            node.UpdateState();
        }
    }

    /// <summary>
    /// 更新指定红点状态
    /// </summary>
    public void Update(RedDotType type)
    {
        var node = GetNode(type);
        node?.UpdateState();
    }

    /// <summary>
    /// 强制设置红点状态（慎用）
    /// </summary>
    public void SetActive(RedDotType type, bool active)
    {
        var node = GetNode(type);
        node?.SetActive(active);
    }

    /// <summary>
    /// 强制设置红点数量（慎用）
    /// </summary>
    public void SetCount(RedDotType type, int count)
    {
        var node = GetNode(type);
        node?.SetCount(count);
    }
    
    public void RegisterStrategy(RedDotNode redDotNode)
    {
        if (_strategies.TryGetValue(redDotNode.Type, out var strategy))
        {
            redDotNode.SetCondition(strategy.ConditionFunc);
            redDotNode.SetCountFunc(strategy.CountFunc);
        }
    }
}