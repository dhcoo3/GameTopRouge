using System;
using System.Collections.Generic;
using AAAGame.ScriptsHotfix.Bridge;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Common;
using AAAGame.ScriptsHotfix.GamePlay.Logic.GameInput;
using AAAGame.ScriptsHotfix.GamePlay.Logic.ProxyManager;
using Fantasy;
using Fantasy.Network;
using GameFramework;
using UnityEngine;
using Log = UnityGameFramework.Runtime.Log;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Frame
{
    /// <summary>
    /// 管理房间模式的权威帧缓存、固定节奏播放、缺帧拉取、追帧和输入发送。
    /// </summary>
    public sealed class FrameProxy : GameProxy
    {
        /// <summary>客户端最多缓存的未执行权威帧数量。</summary>
        private const int MaximumPendingFrameCount = 120;

        /// <summary>连续积压达到该数量时进入快速追帧。</summary>
        private const int CatchUpFrameThreshold = 10;

        /// <summary>单次渲染更新最多执行的追帧数量。</summary>
        private const int MaximumCatchUpFramesPerUpdate = 5;

        /// <summary>同一缺失帧请求的最小间隔，单位为秒。</summary>
        private const float MissingFrameRequestIntervalSeconds = 0.5f;

        /// <summary>缺帧请求在此时间内未获得数据时视为连接异常，单位为秒。</summary>
        private const float MissingFrameTimeoutSeconds = 3f;

        /// <summary>每包输入预留的技能按键数量。</summary>
        private const int OutgoingSkillCapacity = 16;

        /// <summary>当前玩法是否为单机模式。</summary>
        private bool _isLocal = true;

        /// <summary>战斗逻辑世界是否完成加载并能够执行权威帧。</summary>
        private bool _fightReady;

        /// <summary>本渲染帧是否需要发送新的累计确认。</summary>
        private bool _ackDirty;

        /// <summary>负责采集与执行输入的玩法代理。</summary>
        private InputProxy _inputProxy;

        /// <summary>按帧号排序的未执行权威帧缓存。</summary>
        private readonly SortedDictionary<int, CachedFrame> _pendingFrames = new SortedDictionary<int, CachedFrame>();

        /// <summary>构造下一包客户端输入时复用的技能按键列表。</summary>
        private readonly List<KeyCode> _outgoingSkillKeys = new List<KeyCode>(OutgoingSkillCapacity);

        /// <summary>下一帧必须执行的连续权威帧号。</summary>
        private int _nextFrameId = FrameCommandDefine.FirstFrameId;

        /// <summary>客户端已经实际完成模拟的连续帧号。</summary>
        private int _lastExecutedFrameId;

        /// <summary>服务端消息声明的最新权威帧号。</summary>
        private int _latestServerFrameId;

        /// <summary>当前对局标识。</summary>
        private int _matchId;

        /// <summary>当前连接内发送的最后一个输入序号。</summary>
        private int _inputSequence;

        /// <summary>上次以新实时帧为契机发送输入时对应的服务端帧号。</summary>
        private int _lastInputSentForServerFrame;

        /// <summary>用于固定 30Hz 播放的未消费时间，单位为秒。</summary>
        private float _playbackElapsed;

        /// <summary>上次请求缺失帧的起始帧号。</summary>
        private int _lastRequestedFrameId;

        /// <summary>上次请求缺失帧时的实时秒数。</summary>
        private float _lastMissingRequestTime = float.NegativeInfinity;

        /// <summary>当前帧同步播放状态。</summary>
        private FramePlaybackState _state;

        /// <summary>当前是否为单机逻辑。</summary>
        public bool IsLocal => _isLocal;

        /// <summary>客户端已经确认执行的连续帧号。</summary>
        public int LastExecutedFrameId => _lastExecutedFrameId;

        /// <summary>客户端是否正在从历史帧追赶服务端。</summary>
        public bool IsSynchronizing => _state == FramePlaybackState.Buffering || _state == FramePlaybackState.CatchingUp;

        /// <summary>
        /// 初始化帧缓存与事件订阅。
        /// </summary>
        public override void Initialize()
        {
            _isLocal = !GamePlayFacade.Instance.IsRoomMode;
            _inputProxy = GetProxy<InputProxy>();
            ResetFrameState();
            if (_isLocal)
            {
                return;
            }

            Subscribe(GamePlayEvent.EUpdateFrame, OnServerFrameUpdated);
            Subscribe(GamePlayEvent.EFightLoadingFinish, OnFightLoadingFinished);
        }

        /// <summary>
        /// 设置服务端对局描述对应的同步起点；新对局重建缓存，短断线保留执行进度。
        /// </summary>
        /// <param name="matchId">服务端固定下发的对局标识。</param>
        /// <param name="currentFrameId">服务端当前已经封存的最新帧号。</param>
        /// <param name="rebuildFromFirstFrame">是否需要从第一帧重新回放确定性世界。</param>
        public void BeginFrameSynchronization(int matchId, int currentFrameId, bool rebuildFromFirstFrame)
        {
            if (_isLocal || matchId <= 0 || currentFrameId < 0)
            {
                return;
            }

            if (rebuildFromFirstFrame)
            {
                ResetFrameState();
            }

            _matchId = matchId;
            _latestServerFrameId = Math.Max(_latestServerFrameId, currentFrameId);
            _state = FramePlaybackState.Buffering;
            _inputProxy?.SetRoomInputBlocked(true);
            TryRequestMissingFrame();
        }

        /// <summary>
        /// 使用固定播放时钟推进可执行权威帧，并在每帧完成后提交确认。
        /// </summary>
        /// <param name="deltaTime">本次渲染更新经过的非负秒数。</param>
        /// <param name="executeLogicFrame">执行一次玩法确定性逻辑步的回调。</param>
        public void Advance(float deltaTime, Action<fix> executeLogicFrame)
        {
            if (_isLocal || executeLogicFrame == null || !_fightReady || _matchId <= 0)
            {
                return;
            }

            CheckMissingFrameTimeout();
            if (_state == FramePlaybackState.Reconnecting)
            {
                FlushFrameAcknowledgement();
                return;
            }

            if (!HasNextFrame())
            {
                TryRequestMissingFrame();
                FlushFrameAcknowledgement();
                return;
            }

            int pendingCount = GetContinuousFrameCount();
            if (pendingCount >= CatchUpFrameThreshold)
            {
                _state = FramePlaybackState.CatchingUp;
                ExecuteAvailableFrames(MaximumCatchUpFramesPerUpdate, executeLogicFrame);
            }
            else
            {
                _playbackElapsed += Math.Max(0f, deltaTime);
                if (_playbackElapsed >= FrameCommandDefine.LogicFrameRate)
                {
                    _playbackElapsed -= FrameCommandDefine.LogicFrameRate;
                    ExecuteAvailableFrames(1, executeLogicFrame);
                }
            }

            UpdatePlaybackState();
            FlushFrameAcknowledgement();
        }

        /// <summary>
        /// 在对局加载完成后允许补帧与权威帧执行。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">战斗加载完成事件。</param>
        private void OnFightLoadingFinished(object sender, GameEvent e)
        {
            _fightReady = true;
            TryRequestMissingFrame();
        }

        /// <summary>
        /// 缓存服务端推送或补帧响应，并仅对新的实时帧提交一次本地输入。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">包含服务端帧协议的事件。</param>
        private void OnServerFrameUpdated(object sender, GameEvent e)
        {
            G2C_FrameCommand serverCommand = e.GetParam1<G2C_FrameCommand>();
            if (serverCommand == null || serverCommand.FrameId < 0)
            {
                return;
            }

            _latestServerFrameId = Math.Max(_latestServerFrameId, serverCommand.FrameId);
            bool receivedLiveFrame = CacheServerFrames(serverCommand);
            if (_state == FramePlaybackState.WaitingForMissingFrame && HasNextFrame())
            {
                _state = FramePlaybackState.Buffering;
            }

            if (receivedLiveFrame && _state == FramePlaybackState.Playing && _fightReady)
            {
                SendRoomFrameInput(serverCommand.FrameId);
            }

            TryRequestMissingFrame();
        }

        /// <summary>
        /// 将消息中的权威帧复制到私有缓存，返回消息是否包含新的实时末帧。
        /// </summary>
        /// <param name="serverCommand">服务端推送或补帧响应。</param>
        /// <returns>消息包含一条与外层最新帧号相同的新权威帧时返回 true。</returns>
        private bool CacheServerFrames(G2C_FrameCommand serverCommand)
        {
            if (serverCommand.Commands == null || serverCommand.Commands.Count == 0)
            {
                return false;
            }

            bool receivedLiveFrame = false;
            foreach (OneFrameCommand frameCommand in serverCommand.Commands)
            {
                if (frameCommand == null || frameCommand.FrameId < _nextFrameId || _pendingFrames.ContainsKey(frameCommand.FrameId))
                {
                    continue;
                }

                if (_pendingFrames.Count >= MaximumPendingFrameCount)
                {
                    Log.Error("权威帧缓存达到上限 {0}，进入缺帧诊断。", MaximumPendingFrameCount);
                    _state = FramePlaybackState.WaitingForMissingFrame;
                    break;
                }

                _pendingFrames.Add(frameCommand.FrameId, CachedFrame.Create(frameCommand));
                receivedLiveFrame |= frameCommand.FrameId == serverCommand.FrameId;
            }

            return receivedLiveFrame;
        }

        /// <summary>
        /// 在当前状态允许时，以收到的新实时帧为节奏发送一包本地输入。
        /// </summary>
        /// <param name="serverFrameId">触发本次发送的最新服务端帧号。</param>
        private void SendRoomFrameInput(int serverFrameId)
        {
            if (_inputProxy == null || serverFrameId <= _lastInputSentForServerFrame)
            {
                return;
            }

            Session session = GF.GameNetwork.Session;
            if (session == null)
            {
                return;
            }

            _inputProxy.CopyRoomInput(_outgoingSkillKeys, out fix horizontal, out fix vertical);
            int skillCount = _outgoingSkillKeys.Count;
            try
            {
                using C2G_NextFrameCommand message = C2G_NextFrameCommand.Create();
                message.AckFrameId = _lastExecutedFrameId;
                message.InputSequence = ++_inputSequence;
                AddMovementCommand(message, horizontal, vertical);
                AddSkillCommands(message, _outgoingSkillKeys);
                session.C2G_NextFrameCommand(message);
                _inputProxy.ConfirmRoomSkillsSent(skillCount);
                _lastInputSentForServerFrame = serverFrameId;
            }
            catch (Exception exception)
            {
                Log.Error("发送对局 {0} 输入失败：{1}", _matchId, exception.Message);
            }
            finally
            {
                _outgoingSkillKeys.Clear();
            }
        }

        /// <summary>
        /// 向输入消息添加每包必带的移动状态。
        /// </summary>
        /// <param name="message">正在构造的客户端输入协议。</param>
        /// <param name="horizontal">归一化水平移动方向。</param>
        /// <param name="vertical">归一化垂直移动方向。</param>
        private static void AddMovementCommand(C2G_NextFrameCommand message, fix horizontal, fix vertical)
        {
            FrameCommand command = FrameCommand.Create();
            command.CommandType = FrameCommandDefine.MoveCommandType;
            command.x = (int)(horizontal * FrameCommandDefine.CoordinateScale);
            command.y = (int)(vertical * FrameCommandDefine.CoordinateScale);
            message.Commands.Add(command);
        }

        /// <summary>
        /// 向输入消息添加本帧累计的一次性技能按键。
        /// </summary>
        /// <param name="message">正在构造的客户端输入协议。</param>
        /// <param name="skillKeys">需要发送的技能按键。</param>
        private static void AddSkillCommands(C2G_NextFrameCommand message, List<KeyCode> skillKeys)
        {
            foreach (KeyCode key in skillKeys)
            {
                FrameCommand command = FrameCommand.Create();
                command.CommandType = FrameCommandDefine.SkillCommandType;
                command.x = (int)key;
                message.Commands.Add(command);
            }
        }

        /// <summary>
        /// 按固定顺序执行缓存中的若干连续帧。
        /// </summary>
        /// <param name="maximumCount">本次最多允许执行的帧数。</param>
        /// <param name="executeLogicFrame">执行一次玩法确定性逻辑步的回调。</param>
        private void ExecuteAvailableFrames(int maximumCount, Action<fix> executeLogicFrame)
        {
            for (int executedCount = 0; executedCount < maximumCount; executedCount++)
            {
                if (!TryPrepareNextFrame())
                {
                    TryRequestMissingFrame();
                    return;
                }

                executeLogicFrame(FrameCommandDefine.LogicFrameRate);
                CommitExecutedFrame();
                if (!HasNextFrame())
                {
                    return;
                }
            }
        }

        /// <summary>
        /// 将下一连续权威帧转换为逻辑输入，并把对象所有权转交给 InputProxy。
        /// </summary>
        /// <returns>成功准备输入时返回 true。</returns>
        private bool TryPrepareNextFrame()
        {
            if (!_pendingFrames.Remove(_nextFrameId, out CachedFrame frame))
            {
                return false;
            }

            List<InputObj> inputs = CreateFrameInputs(frame);
            if (_inputProxy != null && _inputProxy.TrySetAuthoritativeInputs(inputs))
            {
                return true;
            }

            ReleaseInputObjects(inputs);
            _pendingFrames.Add(frame.FrameId, frame);
            return false;
        }

        /// <summary>
        /// 在确定性逻辑完成后推进连续确认帧号。
        /// </summary>
        private void CommitExecutedFrame()
        {
            _lastExecutedFrameId = _nextFrameId;
            _nextFrameId++;
            _ackDirty = true;
        }

        /// <summary>
        /// 将内部缓存命令转换为本次逻辑步使用的输入对象。
        /// </summary>
        /// <param name="frame">需要执行的权威帧。</param>
        /// <returns>由调用方转交或释放的输入对象列表。</returns>
        private static List<InputObj> CreateFrameInputs(CachedFrame frame)
        {
            List<InputObj> inputs = new List<InputObj>(frame.Commands.Count);
            foreach (CachedCommand command in frame.Commands)
            {
                if (command.CommandType == FrameCommandDefine.MoveCommandType &&
                    command.X >= -FrameCommandDefine.CoordinateScale && command.X <= FrameCommandDefine.CoordinateScale &&
                    command.Y >= -FrameCommandDefine.CoordinateScale && command.Y <= FrameCommandDefine.CoordinateScale)
                {
                    InputObj input = InputObj.Create((fix)command.X / (fix)FrameCommandDefine.CoordinateScale,
                        (fix)command.Y / (fix)FrameCommandDefine.CoordinateScale);
                    input.PlayerId = command.PlayerId;
                    inputs.Add(input);
                }
                else if (command.CommandType == FrameCommandDefine.SkillCommandType)
                {
                    InputObj input = InputObj.Create((KeyCode)command.X);
                    input.PlayerId = command.PlayerId;
                    inputs.Add(input);
                }
            }

            return inputs;
        }

        /// <summary>
        /// 统计从下一帧起可连续执行的缓存帧数。
        /// </summary>
        /// <returns>连续帧数量。</returns>
        private int GetContinuousFrameCount()
        {
            int count = 0;
            while (_pendingFrames.ContainsKey(_nextFrameId + count))
            {
                count++;
            }

            return count;
        }

        /// <summary>
        /// 判断下一连续帧是否已经缓存。
        /// </summary>
        /// <returns>下一帧可执行时返回 true。</returns>
        private bool HasNextFrame()
        {
            return _pendingFrames.ContainsKey(_nextFrameId);
        }

        /// <summary>
        /// 在已知服务端更高帧号且下一帧缺失时发送精确补帧请求。
        /// </summary>
        private void TryRequestMissingFrame()
        {
            if (_isLocal || !_fightReady || _matchId <= 0 || HasNextFrame() || _nextFrameId > _latestServerFrameId)
            {
                return;
            }

            float now = Time.realtimeSinceStartup;
            if (_lastRequestedFrameId == _nextFrameId && now - _lastMissingRequestTime < MissingFrameRequestIntervalSeconds)
            {
                return;
            }

            Session session = GF.GameNetwork.Session;
            if (session == null)
            {
                return;
            }

            try
            {
                using C2G_FrameFetchRequest message = C2G_FrameFetchRequest.Create();
                message.MatchId = _matchId;
                message.StartFrameId = _nextFrameId;
                session.C2G_FrameFetchRequest(message);
                _lastRequestedFrameId = _nextFrameId;
                _lastMissingRequestTime = now;
                _state = FramePlaybackState.WaitingForMissingFrame;
            }
            catch (Exception exception)
            {
                Log.Error("请求对局 {0} 的缺失帧 {1} 失败：{2}", _matchId, _nextFrameId, exception.Message);
            }
        }

        /// <summary>
        /// 在补帧长期无响应时触发网络层重连，并禁止新本地输入混入历史回放。
        /// </summary>
        private void CheckMissingFrameTimeout()
        {
            if (_state != FramePlaybackState.WaitingForMissingFrame ||
                Time.realtimeSinceStartup - _lastMissingRequestTime < MissingFrameTimeoutSeconds)
            {
                return;
            }

            _state = FramePlaybackState.Reconnecting;
            _inputProxy?.SetRoomInputBlocked(true);
            GamePlayToUIBridge.Instance.BeginReconnect();
        }

        /// <summary>
        /// 根据积压数量和追帧完成情况更新播放状态与本地输入开关。
        /// </summary>
        private void UpdatePlaybackState()
        {
            if (_state == FramePlaybackState.Reconnecting || _state == FramePlaybackState.WaitingForMissingFrame)
            {
                return;
            }

            if (_lastExecutedFrameId >= _latestServerFrameId)
            {
                _state = FramePlaybackState.Playing;
                _inputProxy?.SetRoomInputBlocked(false);
                return;
            }

            _state = GetContinuousFrameCount() >= CatchUpFrameThreshold
                ? FramePlaybackState.CatchingUp
                : FramePlaybackState.Buffering;
        }

        /// <summary>
        /// 每个渲染帧最多发送一次最新已执行帧确认。
        /// </summary>
        private void FlushFrameAcknowledgement()
        {
            if (_isLocal || !_ackDirty)
            {
                return;
            }

            Session session = GF.GameNetwork.Session;
            if (session == null)
            {
                return;
            }

            try
            {
                session.C2G_FrameAck(_lastExecutedFrameId);
                _ackDirty = false;
            }
            catch (Exception exception)
            {
                Log.Error("确认逻辑帧 {0} 失败：{1}", _lastExecutedFrameId, exception.Message);
            }
        }

        /// <summary>
        /// 归还因准备失败而未移交 InputProxy 的对象池输入。
        /// </summary>
        /// <param name="inputs">需要归还的输入对象。</param>
        private static void ReleaseInputObjects(List<InputObj> inputs)
        {
            foreach (InputObj input in inputs)
            {
                ReferencePool.Release(input);
            }

            inputs.Clear();
        }

        /// <summary>
        /// 清空帧缓存并恢复未进入对局的初始状态。
        /// </summary>
        private void ResetFrameState()
        {
            _pendingFrames.Clear();
            _outgoingSkillKeys.Clear();
            _nextFrameId = FrameCommandDefine.FirstFrameId;
            _lastExecutedFrameId = FrameCommandDefine.FirstFrameId - 1;
            _latestServerFrameId = FrameCommandDefine.FirstFrameId - 1;
            _matchId = 0;
            _inputSequence = 0;
            _lastInputSentForServerFrame = 0;
            _playbackElapsed = 0f;
            _lastRequestedFrameId = 0;
            _lastMissingRequestTime = float.NegativeInfinity;
            _ackDirty = false;
            _state = FramePlaybackState.WaitingForMatch;
        }

        /// <summary>
        /// 清理帧缓存、输入引用和代理状态。
        /// </summary>
        public override void Clear()
        {
            ResetFrameState();
            _inputProxy = null;
            _fightReady = false;
            _isLocal = true;
            base.Clear();
        }

        /// <summary>
        /// 客户端权威帧播放流程状态。
        /// </summary>
        private enum FramePlaybackState
        {
            /// <summary>尚未收到服务端对局描述。</summary>
            WaitingForMatch,
            /// <summary>正在等待首帧或补齐少量历史帧。</summary>
            Buffering,
            /// <summary>以固定 30Hz 正常播放。</summary>
            Playing,
            /// <summary>下一连续帧缺失，等待服务端补帧响应。</summary>
            WaitingForMissingFrame,
            /// <summary>积压帧过多，按渲染帧上限快速追赶。</summary>
            CatchingUp,
            /// <summary>补帧超时，等待网络组件重连。</summary>
            Reconnecting,
        }

        /// <summary>
        /// 脱离协议对象生命周期保存的权威帧数据。
        /// </summary>
        private sealed class CachedFrame
        {
            /// <summary>权威逻辑帧号。</summary>
            public int FrameId { get; private set; }

            /// <summary>按服务端顺序保存的输入命令。</summary>
            public List<CachedCommand> Commands { get; } = new List<CachedCommand>();

            /// <summary>
            /// 从网络协议深拷贝一帧。
            /// </summary>
            /// <param name="source">收到的服务端帧协议。</param>
            /// <returns>不再依赖协议对象池的私有缓存帧。</returns>
            public static CachedFrame Create(OneFrameCommand source)
            {
                CachedFrame frame = new CachedFrame { FrameId = source.FrameId };
                if (source.UnitFrameCommand == null)
                {
                    return frame;
                }

                foreach (FrameCommand command in source.UnitFrameCommand)
                {
                    if (command != null)
                    {
                        frame.Commands.Add(new CachedCommand(command.Uid, command.CommandType, command.x, command.y));
                    }
                }

                return frame;
            }
        }

        /// <summary>
        /// 脱离协议对象保存的一条帧输入命令。
        /// </summary>
        private readonly struct CachedCommand
        {
            /// <summary>命令归属玩家标识。</summary>
            public int PlayerId { get; }

            /// <summary>命令类型。</summary>
            public int CommandType { get; }

            /// <summary>命令横向参数。</summary>
            public int X { get; }

            /// <summary>命令纵向参数。</summary>
            public int Y { get; }

            /// <summary>
            /// 初始化一条不可变的权威命令副本。
            /// </summary>
            /// <param name="playerId">命令归属玩家标识。</param>
            /// <param name="commandType">命令类型。</param>
            /// <param name="x">命令横向参数。</param>
            /// <param name="y">命令纵向参数。</param>
            public CachedCommand(int playerId, int commandType, int x, int y)
            {
                PlayerId = playerId;
                CommandType = commandType;
                X = x;
                Y = y;
            }
        }
    }
}
