using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Fantasy;
using Fantasy.Network;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.GameNetwork
{
    /// <summary>管理 Fantasy 客户端 Scene、KCP Session、连接回调和心跳传输。</summary>
    public sealed class GameNetworkComponent : GameFrameworkComponent
    {
        /// <summary>单次底层连接超时，单位为毫秒。</summary>
        private const int ConnectionTimeoutMilliseconds = 5000;
        /// <summary>心跳发送周期，单位为毫秒。</summary>
        private const int HeartbeatIntervalMilliseconds = 2000;
        /// <summary>心跳超时，单位为毫秒。</summary>
        private const int HeartbeatTimeoutMilliseconds = 10000;
        /// <summary>检查心跳超时的周期，单位为毫秒。</summary>
        private const int HeartbeatCheckMilliseconds = 1000;
        /// <summary>延迟采样数量。</summary>
        private const int PingSampleCount = 4;

        /// <summary>当前 Fantasy 连接会话。</summary>
        public Session Session { get; private set; }
        /// <summary>当前 Session 尚未释放。</summary>
        public bool IsConnected => Session != null && !Session.IsDisposed;
        /// <summary>连接在建立后断开时通知上层业务，由调用方决定是否重连。</summary>
        public event Action<Session> TransportDisconnected;
        /// <summary>客户端退出或网络组件销毁时通知上层取消业务任务。</summary>
        public event Action TransportShuttingDown;

        /// <summary>本组件独占的 Fantasy 客户端 Scene，隔离全局网络回调。</summary>
        private Scene _scene;
        /// <summary>上一个 Scene 的异步关闭任务。</summary>
        private Task<bool> _sceneClosing = Task.FromResult(true);
        /// <summary>当前连接完成信号。</summary>
        private TaskCompletionSource<bool> _connectedSignal;
        /// <summary>递增连接编号，忽略旧连接的迟到回调。</summary>
        private int _connectionSerial;
        /// <summary>当前是否正在等待底层连接完成。</summary>
        private bool _connecting;
        /// <summary>客户端退出后禁止新连接。</summary>
        private bool _shuttingDown;

        /// <summary>连接到指定 KCP 端点并启动 Fantasy 心跳。</summary>
        /// <param name="host">认证服务返回的游戏服主机名或 IP。</param>
        /// <param name="port">认证服务返回的游戏服端口。</param>
        /// <param name="cancellationToken">取消当前连接的令牌。</param>
        /// <returns>已建立的 Fantasy Session。</returns>
        public async Task<Session> ConnectAsync(string host, int port, CancellationToken cancellationToken)
        {
            if (_shuttingDown) throw new InvalidOperationException("网络组件正在关闭。");
            if (string.IsNullOrWhiteSpace(host) || port <= 0 || port > ushort.MaxValue)
                throw new ArgumentException("游戏服务器地址无效。");

            await WithTimeoutAsync(_sceneClosing, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            Disconnect();
            int serial = _connectionSerial;
            _connecting = true;
            try
            {
                if (UnityEngine.Object.FindObjectOfType<Fantasy.Platform.Unity.Entry>() == null)
                    await Fantasy.Platform.Unity.Entry.Initialize();
                cancellationToken.ThrowIfCancellationRequested();

                if (_scene == null)
                {
                    Scene created = await Scene.Create();
                    if (cancellationToken.IsCancellationRequested || serial != _connectionSerial)
                    {
                        await created.Close();
                        cancellationToken.ThrowIfCancellationRequested();
                        throw new OperationCanceledException();
                    }
                    _scene = created;
                }

                TaskCompletionSource<bool> connected = new TaskCompletionSource<bool>();
                _connectedSignal = connected;
                string address = host.IndexOf(':') >= 0 && !host.StartsWith("[", StringComparison.Ordinal)
                    ? "[" + host + "]:" + port
                    : host + ":" + port;
                Session = _scene.Connect(address, NetworkProtocolType.KCP,
                    () => connected.TrySetResult(true),
                    () => connected.TrySetException(new InvalidOperationException("连接游戏服务器失败。")),
                    () => OnTransportDisconnected(serial), false, ConnectionTimeoutMilliseconds, false);

                await WithTimeoutAsync(connected.Task, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (serial != _connectionSerial || Session == null || Session.IsDisposed)
                    throw new InvalidOperationException("游戏服务器连接已关闭。");

                Session.AddComponent<SessionHeartbeatComponent>().Start(HeartbeatIntervalMilliseconds,
                    HeartbeatTimeoutMilliseconds, HeartbeatCheckMilliseconds, PingSampleCount);
                return Session;
            }
            catch
            {
                if (serial == _connectionSerial) Disconnect();
                throw;
            }
            finally
            {
                if (serial == _connectionSerial) _connecting = false;
            }
        }

        /// <summary>释放当前 KCP Session，保留 Fantasy Scene 供后续业务重新连接。</summary>
        public void Disconnect()
        {
            _connectionSerial++;
            _connecting = false;
            _connectedSignal?.TrySetCanceled();
            _connectedSignal = null;
            Session session = Session;
            Session = null;
            session?.Dispose();
        }

        /// <summary>忽略旧连接回调；上层负责据此决定是否重新认证。</summary>
        /// <param name="serial">回调所属连接编号。</param>
        private void OnTransportDisconnected(int serial)
        {
            if (serial != _connectionSerial) return;
            _connectedSignal?.TrySetException(new InvalidOperationException("游戏服务器连接已断开。"));
            if (_connecting) return;
            Session session = Session;
            if (session != null) TransportDisconnected?.Invoke(session);
        }

        /// <summary>关闭网络 Scene；只在应用退出或组件销毁时调用。</summary>
        private void Shutdown()
        {
            if (_shuttingDown) return;
            _shuttingDown = true;
            TransportShuttingDown?.Invoke();
            Disconnect();
            Scene scene = _scene;
            _scene = null;
            if (scene != null) _sceneClosing = CloseSceneAsync(scene);
        }

        /// <summary>等待旧 Scene 关闭，避免框架复用 Scene 编号时发生交错。</summary>
        /// <param name="scene">本组件创建的客户端 Scene。</param>
        /// <returns>关闭成功后为真。</returns>
        private static async Task<bool> CloseSceneAsync(Scene scene)
        {
            await scene.Close();
            return true;
        }

        /// <summary>为底层连接等待设置上限，并观察超时后迟到任务的异常。</summary>
        /// <typeparam name="T">结果类型。</typeparam>
        /// <param name="task">需要等待的任务。</param>
        /// <param name="cancellationToken">调用方取消令牌。</param>
        /// <returns>成功完成的结果。</returns>
        private static async Task<T> WithTimeoutAsync<T>(Task<T> task, CancellationToken cancellationToken)
        {
            using (CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                Task delay = Task.Delay(ConnectionTimeoutMilliseconds, timeout.Token);
                if (await Task.WhenAny(task, delay) != task)
                {
                    _ = task.ContinueWith(failed => { _ = failed.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
                    cancellationToken.ThrowIfCancellationRequested();
                    throw new TimeoutException("游戏服务器连接超时。");
                }
                timeout.Cancel();
                return await task;
            }
        }

        /// <summary>应用关闭时同步释放 Session 与客户端 Scene。</summary>
        private void OnApplicationQuit() => Shutdown();

        /// <summary>组件销毁后禁止新连接并释放传输资源。</summary>
        private void OnDestroy() => Shutdown();
    }
}
