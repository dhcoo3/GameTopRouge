using System;
using System.Collections.Generic;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Role;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Role;
using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;

///<summary>
///动画播放的管理器，是典型的ARPG之类即时回合制使用的，用来管理当前应该播放那个动画
///不仅仅是角色，包括aoe、bullet等，只要需要管理播放什么动画（带有animator的gameobject）就应该用这个
///</summary>
public class UnitAnim : MonoBehaviour
{
    private Animator _animator;

    public Animator Animator
    {
        get
        {
            return _animator ??= GetComponent<Animator>();
        }
    }
    
    public RoleBehaviour RoleBehaviour;

    ///<summary>
    ///播放的倍速，作用于每个信息的duration减少速度
    ///</summary>
    public fix timeScale = 1;

    ///<summary>
    ///动画的逻辑信息
    ///key其实就是要播放的动画的key，比如“attack”等。
    ///value则是一个animInfo，取其RandomKey()的值就可以得到要播放的动画在animator中的名称（play()的参数）
    ///</summary>
    public Dictionary<cfg.Anim.Type, AnimInfo> animInfo = new Dictionary<cfg.Anim.Type, AnimInfo>();

    //当前正在播放的动画的权重，只有权重>=这个值才会切换动画
    private AnimInfo playingAnim = null;

    //当前权重持续时间（单位秒），归0后，currentPriority归0
    private fix priorityDuration = 0;

    private int currentAnimPriority
    {
        get { return playingAnim == null ? 0 : (priorityDuration <= 0 ? 0 : playingAnim.priority); }
    }

    private cfg.Anim.Direction playDirection = cfg.Anim.Direction.Right;

    public void LogicUpdate(float deltaTime)
    {
        if (!Animator || animInfo == null || animInfo.Count <= 0) return;

        if (priorityDuration > 0)
        {
            priorityDuration -= deltaTime * timeScale;
        }
        
        if (RoleBehaviour !=null && RoleBehaviour.WaitPlayAnim.Count > 0)
        {
            for (int i = 0; i < RoleBehaviour.WaitPlayAnim.Count; i++)
            {
                AnimPlayData animPlayData = RoleBehaviour.WaitPlayAnim[i];
                Play(animPlayData);
                ReferencePool.Release(animPlayData);
            }
            RoleBehaviour.WaitPlayAnim.Clear();
            //Log.Info("Anim LogicUpdate {0} {1}",RoleBehaviour.WaitPlayAnim.Count,DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        }
    }
    
    ///<summary>
    ///申请播放某个动画，不是你申请就鸟你了，要看有什么正在播放的
    ///<param name="animName">动画的名称，对应animInfo的key</param>
    ///</summary>
    public void Play(AnimPlayData animPlayData)
    {
        //Log.Info("Play Anim Data11 {0} {1} {2}",animPlayData.AnimType,animPlayData.Direction,DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        if (animInfo.ContainsKey(animPlayData.AnimType) == false || Animator == null) return;
        //Log.Info("Play Anim Data22 {0} {1} {2}",animPlayData.AnimType,animPlayData.Direction,DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        if (playingAnim != null && playingAnim.key == animPlayData.AnimType && playDirection == animPlayData.Direction) return; //已经在播放了
        //Log.Info("Play Anim Data33 {0} {1} {2}",animPlayData.AnimType,animPlayData.Direction,DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        AnimInfo toPlay = animInfo[animPlayData.AnimType];
        if (currentAnimPriority > toPlay.priority) return;   //优先级不够不放
        SingleAnimInfo playOne = toPlay.RandomKey(animPlayData.Direction);
        if (string.IsNullOrEmpty(playOne.animName)) return;
        playDirection = playOne.direction;
        timeScale = animPlayData.TimeScale;
        Animator.speed = timeScale;
        //Log.Info("Play Anim Data44 {0} {1} {2}",animPlayData.AnimType,animPlayData.Direction,DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        Animator.Play(playOne.animName);
        playingAnim = toPlay;
        priorityDuration = playOne.duration;
    }
}

