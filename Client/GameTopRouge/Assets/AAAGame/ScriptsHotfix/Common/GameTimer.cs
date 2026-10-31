using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;


public class GameTimer : IReference, IDisposable
{
    public enum TimerType
    {
        Update,     // 每帧更新
        Realtime,   // 真实时间（不受Time.timeScale影响）
        FixedUpdate // 固定帧率更新
    }

    private bool _isRunning;
    private float _interval;
    private float _elapsed;
    private int _loopCount;
    private int _currentLoop;
    private TimerType _timerType = TimerType.Update;
    private Action<float> _onTick;
    private Action _onCompleted;
    
    /// <summary>
    /// Mono销毁监听
    /// </summary>
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    
    private bool _isDisposed;

    /// <summary>
    /// 启动定时器
    /// </summary>
    public void Start()
    {
        if (_isRunning || _isDisposed || _cancellationTokenSource.IsCancellationRequested) return;
        
        _isRunning = true;
        _elapsed = 0f;
        _currentLoop = 0;
        
        RunTimer().Forget();
    }

    private async UniTaskVoid RunTimer()
    {
        try
        {
            while (_isRunning && (_loopCount == -1 || _currentLoop < _loopCount))
            {
                await WaitForNextTick();

                if (!_isRunning || _cancellationTokenSource.IsCancellationRequested) break;

                _onTick?.Invoke(_elapsed);
                _elapsed = 0f;
                _currentLoop++;
            }

            if (_isRunning && !_cancellationTokenSource.IsCancellationRequested)
            {
                _onCompleted?.Invoke();
            }
        }
        catch (OperationCanceledException)
        {

            // 定时器被取消
            Debug.Log("GameTimer is canceled.");
        }
        finally
        {
            Dispose();
        }
    }

    private async UniTask WaitForNextTick()
    {
        while (_elapsed < _interval && _isRunning && !_cancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                switch (_timerType)
                {
                    case TimerType.Update:
                        await UniTask.Yield(_cancellationTokenSource.Token);
                        _elapsed += Time.deltaTime;
                        break;
                    case TimerType.Realtime:
                        await UniTask.Yield(PlayerLoopTiming.Update, _cancellationTokenSource.Token);
                        _elapsed += Time.unscaledDeltaTime;
                        break;
                    case TimerType.FixedUpdate:
                        await UniTask.WaitForFixedUpdate(_cancellationTokenSource.Token);
                        _elapsed += Time.fixedDeltaTime;
                        break;
                }
            }
            catch (Exception e)
            {
               Log.Info(e);
               Dispose();
               break;
            }
        }
    }

    /// <summary>
    /// 停止定时器
    /// </summary>
    public void Stop()
    {
        if (!_isRunning || _isDisposed) return;
        
        _isRunning = false;
        _cancellationTokenSource.Cancel();
    }

    /// <summary>
    /// 重置定时器
    /// </summary>
    public void Reset()
    {
        if (_isDisposed) return;
        
        Stop();
        _elapsed = 0f;
        _currentLoop = 0;
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        Debug.Log("GameTimer is Dispose.");
        Stop();
        _isDisposed = true;
        _cancellationTokenSource?.Dispose();
        _onTick = null;
        _onCompleted = null;
        ReferencePool.Release(this);
    }

    public void Clear()
    {
      
    }

    /// <summary>
    /// 延时定时器
    /// </summary>
    /// <param name="interval"></param>
    /// <param name="onTick"></param>
    /// <param name="onCompleted"></param>
    /// <returns></returns>
    public static GameTimer CreateInterval(float interval, Action onCompleted = null,TimerType timerType = TimerType.Update)
    {
        GameTimer timer = ReferencePool.Acquire<GameTimer>();
        timer._interval = interval;
        timer._timerType = timerType;
        timer._loopCount = 1;
        timer._onCompleted = onCompleted;
        return timer;
    } 
    
    /// <summary>
    /// 延时定时器，Mono对象专用，Mono上的的定时器需要绑定，销毁时需要清除掉定时器
    /// </summary>
    /// <param name="mono"></param>
    /// <param name="interval"></param>
    /// <param name="onTick"></param>
    /// <param name="onCompleted"></param>
    /// <returns></returns>
    public static GameTimer CreateInterval(MonoBehaviour mono, float interval,Action onCompleted = null,TimerType timerType = TimerType.Update)
    {
        GameTimer timer = ReferencePool.Acquire<GameTimer>();
        timer._interval = interval;
        timer._timerType = timerType;
        timer._loopCount = 1;
        timer._onCompleted = onCompleted;
        
        // 如果传入了有效的lifeTimeToken，注册销毁时的回调
        CancellationToken cts = mono.GetCancellationTokenOnDestroy();
        timer._cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cts);
        if (cts != default && cts.CanBeCanceled)
        {
            cts.Register(() =>
            {
                if (!timer._isDisposed)
                {
                    timer.Dispose();
                }
            });
        }
        
        return timer;
    }

    /// <summary>
    /// 循环定时器
    /// </summary>
    /// <param name="loopCount"></param>
    /// <param name="interval"></param>
    /// <param name="onTick"></param>
    /// <param name="onCompleted"></param>
    /// <returns></returns>
    public static GameTimer CreateLoop(int loopCount,float interval = 0,Action<float> onTick = null,Action onCompleted = null,TimerType timerType = TimerType.Update)
    {
        GameTimer timer = ReferencePool.Acquire<GameTimer>();
        timer._loopCount = loopCount;
        timer._interval = interval;
        timer._timerType = timerType;
        timer._onTick = onTick;
        timer._onCompleted = onCompleted;
        return timer;
    } 
    
    /// <summary>
    /// 循环定时器，Mono对象专用，Mono上的的定时器需要绑定，销毁时需要清除掉定时器
    /// </summary>
    /// <param name="mono"></param>
    /// <param name="loopCount"></param>
    /// <param name="interval"></param>
    /// <param name="onTick"></param>
    /// <param name="onCompleted"></param>
    /// <returns></returns>
    public static GameTimer CreateLoop(MonoBehaviour mono, int loopCount,float interval = 0,Action<float> onTick = null,Action onCompleted = null,TimerType timerType = TimerType.Update)
    {
        GameTimer timer = ReferencePool.Acquire<GameTimer>();
        timer._loopCount = loopCount;
        timer._interval = interval;
        timer._timerType = timerType;
        timer._onTick = onTick;
        timer._onCompleted = onCompleted;
        
        // 如果传入了有效的lifeTimeToken，注册销毁时的回调
        CancellationToken cts = mono.GetCancellationTokenOnDestroy();
        timer._cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cts);
        if (cts != default && cts.CanBeCanceled)
        {
            cts.Register(() =>
            {
                if (!timer._isDisposed)
                {
                    timer.Dispose();
                }
            });
        }
        
        return timer;
    } 
    
    
    /// <summary>
    /// 无限循环定时器
    /// </summary>
    /// <param name="loopCount"></param>
    /// <param name="interval"></param>
    /// <param name="onTick"></param>
    /// <param name="onCompleted"></param>
    /// <returns></returns>
    public static GameTimer CreateUpdate(Action<float> onTick = null,Action onCompleted = null,TimerType timerType = TimerType.Update)
    {
        GameTimer timer = ReferencePool.Acquire<GameTimer>();
        timer._loopCount = -1;
        timer._interval = 0;
        timer._timerType = timerType;
        timer._onTick = onTick;
        timer._onCompleted = onCompleted;
        return timer;
    } 
}