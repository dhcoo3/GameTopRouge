using System.Collections.Generic;
using cfg.Anim;
using UnityEngine;

///<summary>
///单个动画的信息
///</summary>
public class AnimInfo{
    ///<summary>
    ///这个动画的key，相当于一个id
    ///</summary>
    public cfg.Anim.Type key;

    ///<summary>
    ///动画优先级，因为本质是个回合制游戏，所以播放动作有优先级高的优先播放一说。
    ///比如受伤通常就会低于攻击，因为角色很可能同时受伤和发动攻击，但是发动攻击并不会因为受伤终止，比如wow里面。
    ///</summary>
    public int priority;
    
    public Dictionary<cfg.Anim.Direction,List<SingleAnimInfo>> allAnims = new Dictionary<cfg.Anim.Direction, List<SingleAnimInfo>>();

    public AnimInfo(cfg.Anim.Type key, int priority = 0){
        this.priority = priority;
        this.key = key;
    }

    ///<summary>
    ///随机获得一个动画信息（animator里的动画名字等）
    ///<return>动画信息</return>
    ///</summary>
    public SingleAnimInfo RandomKey(cfg.Anim.Direction direction)
    {
        if (allAnims.TryGetValue(direction, out List<SingleAnimInfo> anims))
        {
            if (anims.Count <= 0) return SingleAnimInfo.Null;
            
            if (anims.Count == 1) return anims[0];
            
            /*
           int totalV = 0;
           for (int i = 0; i < animations.Length; i++){
               totalV += animations[i].Value;
           }

           if (totalV <= 0) return SingleAnimInfo.Null;


           int rv = Random.Range(0, totalV);

           int rIndex = 0;
           while (rv > 0){
               rv -= animations[rIndex].Value;
               rIndex += 1;
           }

           rIndex = fixMath.min(rIndex, animations.Length - 1);
           return animations[rIndex].Key;*/
        }

        return SingleAnimInfo.Null;
    }
}

///<summary>
///单个动画信息，主要是在animator中的name，以及多久以后回到可以被改写的程度
///</summary>
public struct SingleAnimInfo{
    ///<summary>
    ///animator中的名称
    ///</summary>
    public string animName;

    ///<summary>
    ///在多久之后权重清0，单位秒
    ///</summary>
    public fix duration;

    public int priority;
    
    public cfg.Anim.Direction direction;

    public SingleAnimInfo(string animName, int priority ,fix duration,cfg.Anim.Direction direction = Direction.Right){
        this.animName = animName;
        this.duration = duration;
        this.priority = priority;
        this.direction = direction;
    }
    
    public static SingleAnimInfo Null = new SingleAnimInfo("", 0,fix.Zero);
}