using System;
using System.Linq;
using UnityEngine;
using GameFramework;
using UnityGameFramework.Runtime;
using DG.Tweening;
using TMPro;
public static class EntityExtension
{
    /// <summary>
    /// 创建Entity
    /// </summary>
    /// <param name="eCom"></param>
    /// <param name="pfbName">预制体资源名(相对于Assets/AAAGame/Prefabs/Entity目录)</param>
    /// <param name="logicName">Entity逻辑脚本名</param>
    /// <param name="eGroup">Entity所属的组(Const.EntityGroup枚举)</param>
    /// <param name="priority">异步加载优先级</param>
    /// <param name="parms">Entity参数(必须)</param>
    /// <returns>Entity Id</returns>
    public static int ShowEntity(this EntityComponent eCom, string pfbName, string logicName, Const.EntityGroup eGroup, int priority, EntityParams parms)
    {
        var eId = parms.Id;
        var assetFullName = UtilityBuiltin.AssetsPath.GetEntityPath(pfbName);
        eCom.ShowEntity(eId, Type.GetType(logicName), assetFullName, eGroup.ToString(), priority, parms);
        return eId;
    }

    /// <summary>
    /// 创建Entity
    /// </summary>
    /// <param name="eCom"></param>
    /// <param name="pfbName">预制体资源名(相对于Assets/AAAGame/Prefabs/Entity目录)</param>
    /// <param name="logicName">Entity逻辑脚本名</param>
    /// <param name="eGroup">Entity所属的组(Const.EntityGroup枚举)</param>
    /// <param name="parms">Entity参数(必须)</param>
    /// <returns>Entity Id</returns>
    public static int ShowEntity(this EntityComponent eCom, string pfbName, string logicName, Const.EntityGroup eGroup, EntityParams parms)
    {
        return eCom.ShowEntity(pfbName, logicName, eGroup, 0, parms);
    }

    /// <summary>
    /// 创建Entity
    /// </summary>
    /// <typeparam name="T">Entity逻辑脚本类型</typeparam>
    /// <param name="eCom"></param>
    /// <param name="pfbName">预制体资源名(相对于Assets/AAAGame/Prefabs/Entity目录)</param>
    /// <param name="eGroup">Entity所属的组(Const.EntityGroup枚举)</param>
    /// <param name="priority">异步加载优先级</param>
    /// <param name="parms">Entity参数(必须)</param>
    /// <returns>Entity Id</returns>
    public static int ShowEntity<T>(this EntityComponent eCom, string pfbName, Const.EntityGroup eGroup, int priority, EntityParams parms) where T : EntityLogic
    {
        var eId = parms.Id;
        var assetFullName = UtilityBuiltin.AssetsPath.GetEntityPath(pfbName);
        eCom.ShowEntity<T>(eId, assetFullName, eGroup.ToString(), priority, parms);
        return eId;
    }

    /// <summary>
    /// 创建Entity
    /// </summary>
    /// <typeparam name="T">Entity逻辑脚本类型</typeparam>
    /// <param name="eCom"></param>
    /// <param name="pfbName">预制体资源名(相对于Assets/AAAGame/Prefabs/Entity目录)</param>
    /// <param name="eGroup">Entity所属的组(Const.EntityGroup枚举)</param>
    /// <param name="parms">Entity参数(必须)</param>
    /// <returns>Entity Id</returns>
    public static int ShowEntity<T>(this EntityComponent eCom, string pfbName, Const.EntityGroup eGroup, EntityParams parms) where T : EntityLogic
    {
        return eCom.ShowEntity<T>(pfbName, eGroup, 0, parms);
    }

    /// <summary>
    /// 隐藏一个Entity组下所有Entities
    /// </summary>
    /// <param name="eCom"></param>
    /// <param name="groupName"></param>
    public static void HideGroup(this EntityComponent eCom, string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
        {
            Log.Warning("Entity Group Is Null Or WhiteSpace");
            return;
        }
        var eGroup = eCom.GetEntityGroup(groupName);
        var all = eGroup.GetAllEntities();

        foreach (Entity e in all)
        {
            eCom.HideEntity(e);
        }
    }
    /// <summary>
    /// 隐藏Entity(带有安全检测, 无需判空)
    /// </summary>
    /// <param name="eCom"></param>
    /// <param name="entityId"></param>
    public static void HideEntitySafe(this EntityComponent eCom, int entityId)
    {
        if (eCom.IsLoadingEntity(entityId))
        {
            GF.VariablePool.ClearVariables(entityId);

            eCom.HideEntity(entityId);
            return;
        }
        if (eCom.HasEntity(entityId))
        {
            eCom.HideEntity(entityId);
        }
    }
    /// <summary>
    /// 隐藏Entity(带有安全检测, 无需判空)
    /// </summary>
    /// <param name="eCom"></param>
    /// <param name="logic"></param>
    public static void HideEntitySafe(this EntityComponent eCom, EntityLogic logic)
    {
        if (logic != null && logic.Available)
        {
            eCom.HideEntity(logic.Entity);
        }
    }
    /// <summary>
    /// 获取Entity的逻辑脚本
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="eCom"></param>
    /// <param name="eId"></param>
    /// <returns></returns>
    public static T GetEntity<T>(this EntityComponent eCom, int eId) where T : EntityLogic
    {
        if (!eCom.HasEntity(eId)) return null;

        var eLogic = eCom.GetEntity(eId).Logic as T;
        return eLogic;
    }
}
