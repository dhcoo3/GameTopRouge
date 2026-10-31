using System;
using System.Collections;
using GameFramework;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Aniamtor辅助类
/// </summary>
public class AnimatorHelper : MonoBehaviour
{
    private int m_InstanceId = 0;
    
    private Animator m_Animator;

    public Animator Animator
    {
        get
        {
            return m_Animator ??= this.GetComponent<Animator>();
        }
    }

    private GameFrameworkAction m_CsCallBack;

    private bool m_AnimStart = false;
    private string m_AnimName = "";

    //自定义动画曲线
    public List<AnimationCurve> AnimationCurves = new List<AnimationCurve>();

    public List<AudioClip> m_AudioClip = new List<AudioClip>();

    AnimationClip[] m_AnimationClips { get
        {
            return Animator?.runtimeAnimatorController.animationClips;
        }
    }

    private void Reset()
    {
        if (Animator)
        {
           
        }
    }

    /// <summary>
    /// 激活时是否自动开启Animator
    /// </summary>
    public bool StartActivateAnimator = false;

    private void Awake()
    {
        m_InstanceId = GetInstanceID();
        
        if (!StartActivateAnimator && Animator)
        {
            Stop();
        }         
    }

    private void OnDestroy()
    {
       // m_CallBackListener.Release();
    }

    private void LateUpdate()
    {
        if (m_AnimStart && Animator)
        {
            AnimatorStateInfo stateinfo = Animator.GetCurrentAnimatorStateInfo(0);
   
            
            if (stateinfo.IsName(m_AnimName) && stateinfo.normalizedTime >= 1.0f)
            {
                m_AnimStart = false;
                PlayFinish();
            }
        }
    }   

    private bool _AniamtionAutoClose;
    public bool m_AniamtionAutoClose
    {
        get { return _AniamtionAutoClose; }
        set { _AniamtionAutoClose = value; }
    }

    public float GetAnimTime(string stateName)
    {
        float length = 0;

        foreach (AnimationClip clip in m_AnimationClips)
        {
            if (clip.name.Equals(stateName))
            {
                length = clip.length;
                break;
            }
        }

        return length;
    }
    
    /// <summary>
    /// 播放一个动画
    /// </summary>
    /// <param name="name">动画状态名称</param>
    /// <param name="speed">播放速度 </param>
    /// <param name="autoStop">播放完毕后是否设置Animtor为未激活</param>
    /// <param name="luaFunction">lua回调</param>
    /// <param name="gameFrameworkAction">C#回调</param>
    /// <param name="starttime">播放起始时间</param>
    /// <returns></returns>
    public float Play(string name, float speed = 1, bool autoStop = false, GameFrameworkAction gameFrameworkAction = null, float starttime = 0)
    {         
        m_AnimStart = false;
        
        //UIFramework.Base.StartCoroutine(PlayAnim(name, 1, autoStop, gameFrameworkAction, starttime));
      
        return GetAnimTime(name) - starttime;
    }

    public IEnumerator PlayAnim(string name, float speed = 1, bool autoStop = false, GameFrameworkAction gameFrameworkAction = null, float starttime = 0)
    {
        if (Animator && Animator.runtimeAnimatorController)
        {            
            Animator.enabled = true;
            yield return new WaitForSeconds(0.01f);

            if (gameObject != null)
            {
                if (Animator.HasState(0, Animator.StringToHash(name)))
                {
                    m_CsCallBack = gameFrameworkAction;
                    m_AnimName = name;
                    m_AniamtionAutoClose = autoStop;
                    Animator.speed = speed;
                    Animator.Play(name, 0, starttime);   
                    m_AnimStart = true;
                }
                else
                {                 
                    gameFrameworkAction?.Invoke();
                    Debug.LogFormat("尝试播放一个空动画:" + name);
                }
            }
        }
        else
        {
            if (gameObject != null)
            {
                gameFrameworkAction?.Invoke();
                Debug.LogFormat("没有Animator组件或动画，尝试播放动画:" + name);
            }
        }
    }
    

    /// <summary>
    /// 播放完成处理
    /// </summary>
    public void PlayFinish()
    {
        if(m_CsCallBack != null)
        {
            m_CsCallBack.Invoke();
        }  

        if (m_AniamtionAutoClose)
        {
            Stop();
        }
    }     

    /// <summary>
    /// 设置Animaotr为未激活
    /// </summary>
    public void Stop()
    {
        if (Animator)
        {
            Animator.enabled = false;
        }
    }

    /// <summary>
    /// 激活Animaotr
    /// </summary>
    public void Enabled()
    {
        if (Animator)
        {
            Animator.enabled = true;
        }
    }

    /// <summary>
    /// 设置自身激活状态
    /// </summary>
    /// <param name="active"></param>
    public void SetActive(bool active)
    {
        if(gameObject == null) return;
        this.gameObject.SetActive(active);
    }

    /// <summary>
    /// 获取一个自宝义动画曲线
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public AnimationCurve GetAnimationCurve(int index)
    {
        if(index < AnimationCurves.Count)
        {
            return AnimationCurves[index];
        }

        return null;
    }

    /// <summary>
    /// 帧事件1
    /// </summary>
    public void FrameEvent1()
    {
     
    }

    /// <summary>
    /// 帧事件2
    /// </summary>
    public void FrameEvent2()
    {
     
    }

    /// <summary>
    /// 帧事件3
    /// </summary>
    public void FrameEvent3()
    {
      
    }

    /// <summary>
    /// 设置帧事件1
    /// </summary>
    public void SetFrameEvent1(GameFrameworkAction callBack)
    {
     
    }

    /// <summary>
    /// 设置帧事件2
    /// </summary>
    public void SetFrameEvent2(GameFrameworkAction callBack)
    {
      
    }

    /// <summary>
    /// 设置帧事件3
    /// </summary>
    public void SetFrameEvent3(GameFrameworkAction callBack)
    {
     
    }

    /// <summary>
    /// 播放声音
    /// </summary>
    /// <param name="soundName"></param>
    public void PlaySound(string soundName)
    {

    }
}

