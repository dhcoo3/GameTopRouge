using System.Collections.Generic;
using AAAGame.ScriptsHotfix.GamePlay.Logic.ProxyManager;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit;
using Fantasy;
using GameFramework;
using UnityEngine;
using Log = UnityGameFramework.Runtime.Log;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.GameInput
{
    /// <summary>
    /// 采集本地操作并在逻辑步中执行已准备的权威输入。
    /// </summary>
    public class InputProxy : GameProxy
    {
        /// <summary>
        /// 初始化 InputObj 对象池和输入列表时预留的对象数量。
        /// </summary>
        private const int InputObjectPoolCapacity = 16;

        /// <summary>
        /// 单个服务端帧之间最多缓存的本地技能次数，防止异常 UI 输入无限增长。
        /// </summary>
        private const int MaximumPendingSkillCount = 16;

        /// <summary>
        /// 本逻辑帧等待执行的输入对象；其中对象所有权属于本代理。
        /// </summary>
        private List<InputObj> _inputObjs;

        /// <summary>
        /// 尚未随下一包帧输入发送的一次性技能按键。
        /// </summary>
        private List<KeyCode> _pendingSkillKeys;

        /// <summary>
        /// 将输入应用到角色单位的逻辑代理。
        /// </summary>
        private UnitProxy _unitProxy;

        /// <summary>
        /// 是否已由 FrameProxy 准备一组权威帧输入，并等待本次逻辑步执行。
        /// </summary>
        private bool _hasPreparedFrameInput;

        /// <summary>
        /// 房间模式中最新的归一化水平移动方向。
        /// </summary>
        private fix _roomHorizontal;

        /// <summary>
        /// 房间模式中最新的归一化垂直移动方向。
        /// </summary>
        private fix _roomVertical;

        /// <summary>
        /// 重建或追赶历史帧时阻止新的本地操作进入权威输入流。
        /// </summary>
        private bool _roomInputBlocked;
        /// <summary>本地剧情会话独立占用输入，不影响权威帧执行。</summary>
        private bool _dialogueInputBlocked;

        /// <summary>设置本地对话输入限制并清除尚未提交的操作。</summary>
        /// <param name="blocked">是否禁止移动和技能采集。</param>
        public void SetDialogueInputBlocked(bool blocked)
        {
            _dialogueInputBlocked = blocked;
            if (!blocked) return;
            _roomHorizontal = _roomVertical = 0;
            _pendingSkillKeys?.Clear();
            if (!IsRoomMode())
            {
                ReleaseInputObjects(_inputObjs);
                // 立即提交停止移动，避免对话遮住摇杆后角色沿旧方向继续行走。
                if (_inputObjs != null) AddLocalMovementInput(0, 0);
            }
        }

        /// <summary>
        /// 初始化输入缓存、关联代理和 InputObj 对象池。
        /// </summary>
        public override void Initialize()
        {
            ResetInputState();
            _inputObjs = new List<InputObj>(InputObjectPoolCapacity);
            _pendingSkillKeys = new List<KeyCode>(MaximumPendingSkillCount);
            _unitProxy = GetProxy<UnitProxy>();
            ReferencePool.Add<InputObj>(InputObjectPoolCapacity);
        }

        /// <summary>
        /// 在本次逻辑帧中执行已准备的输入对象。
        /// </summary>
        /// <param name="deltaTime">当前固定逻辑步长；输入执行不依赖该值。</param>
        public override void LogicUpdate(fix deltaTime)
        {
            if (IsRoomMode() && !_hasPreparedFrameInput)
            {
                return;
            }

            ExecutePendingInputs();
            _hasPreparedFrameInput = false;
        }

        /// <summary>
        /// 接收摇杆或方向键产生的移动方向。
        /// </summary>
        /// <param name="horizontal">归一化水平移动方向，合法范围为 [-1, 1]。</param>
        /// <param name="vertical">归一化垂直移动方向，合法范围为 [-1, 1]。</param>
        public void Input(float horizontal, float vertical)
        {
            if (_dialogueInputBlocked) { horizontal = 0; vertical = 0; }
            if (IsRoomMode())
            {
                if (_roomInputBlocked)
                {
                    return;
                }

                CacheRoomMovement(horizontal, vertical);
                return;
            }

            AddLocalMovementInput(horizontal, vertical);
        }

        /// <summary>
        /// 接收一次性技能按键。
        /// </summary>
        /// <param name="key">要执行的技能按键；KeyCode.None 会被忽略。</param>
        public void Input(KeyCode key)
        {
            if (_dialogueInputBlocked) return;
            if (IsRoomMode())
            {
                if (_roomInputBlocked)
                {
                    return;
                }

                CacheRoomSkill(key);
                return;
            }

            AddLocalSkillInput(key);
        }

        /// <summary>
        /// 接收 FrameProxy 转交的一组连续服务端权威输入。
        /// </summary>
        /// <param name="inputObjs">该帧的输入列表；成功时对象所有权转移给本代理。</param>
        /// <returns>成功接收并等待逻辑步执行时返回 true。</returns>
        internal bool TrySetAuthoritativeInputs(List<InputObj> inputObjs)
        {
            if (inputObjs == null)
            {
                Log.Error("收到空的服务端权威输入列表。");
                return false;
            }

            if (_hasPreparedFrameInput)
            {
                Log.Error("上一组权威输入尚未执行，拒绝覆盖。");
                return false;
            }

            _inputObjs.AddRange(inputObjs);
            inputObjs.Clear();
            _hasPreparedFrameInput = true;
            return true;
        }

        /// <summary>
        /// 复制当前房间输入状态，供 FrameProxy 构造下一帧网络包。
        /// </summary>
        /// <param name="skillKeys">接收待发送技能按键的列表；调用前内容会被清空。</param>
        /// <param name="horizontal">返回最新归一化水平移动方向。</param>
        /// <param name="vertical">返回最新归一化垂直移动方向。</param>
        internal void CopyRoomInput(List<KeyCode> skillKeys, out fix horizontal, out fix vertical)
        {
            horizontal = _roomHorizontal;
            vertical = _roomVertical;
            if (skillKeys == null)
            {
                return;
            }

            skillKeys.Clear();
            skillKeys.AddRange(_pendingSkillKeys);
        }

        /// <summary>
        /// 移除已由 FrameProxy 成功发送的技能输入，保留发送期间新增的输入。
        /// </summary>
        /// <param name="skillCount">本次成功发送的技能数量。</param>
        internal void ConfirmRoomSkillsSent(int skillCount)
        {
            if (skillCount <= 0)
            {
                return;
            }

            int removeCount = System.Math.Min(skillCount, _pendingSkillKeys.Count);
            _pendingSkillKeys.RemoveRange(0, removeCount);
        }

        /// <summary>
        /// 由 FrameProxy 在追帧期间阻止本地输入，追平后恢复采集。
        /// </summary>
        internal void SetRoomInputBlocked(bool blocked)
        {
            _roomInputBlocked = blocked;
            if (!blocked)
            {
                return;
            }

            _roomHorizontal = 0;
            _roomVertical = 0;
            _pendingSkillKeys.Clear();
        }

        /// <summary>
        /// 判断当前玩法是否运行房间帧同步逻辑。
        /// </summary>
        /// <returns>处于房间模式时返回 true。</returns>
        private static bool IsRoomMode()
        {
            return GamePlayFacade.Instance.IsRoomMode;
        }

        /// <summary>
        /// 缓存房间模式的最新移动状态，等待 FrameProxy 收到下一服务端帧后发送。
        /// </summary>
        /// <param name="horizontal">归一化水平移动方向。</param>
        /// <param name="vertical">归一化垂直移动方向。</param>
        private void CacheRoomMovement(float horizontal, float vertical)
        {
            _roomHorizontal = (fix)horizontal;
            _roomVertical = (fix)vertical;
        }

        /// <summary>
        /// 缓存一次性技能按键，并限制未发送技能数量。
        /// </summary>
        /// <param name="key">要缓存的技能按键。</param>
        private void CacheRoomSkill(KeyCode key)
        {
            if (key == KeyCode.None)
            {
                return;
            }

            if (_pendingSkillKeys.Count >= MaximumPendingSkillCount)
            {
                Log.Warning("待发送技能输入已达到上限，忽略按键 {0}。", key);
                return;
            }

            _pendingSkillKeys.Add(key);
        }

        /// <summary>
        /// 创建并缓存单机模式的移动输入对象。
        /// </summary>
        /// <param name="horizontal">归一化水平移动方向。</param>
        /// <param name="vertical">归一化垂直移动方向。</param>
        private void AddLocalMovementInput(float horizontal, float vertical)
        {
            InputObj inputObj = InputObj.Create((fix)horizontal, (fix)vertical);
            inputObj.PlayerId = GF.UserId;
            _inputObjs.Add(inputObj);
        }

        /// <summary>
        /// 创建并缓存单机模式的技能输入对象。
        /// </summary>
        /// <param name="key">要执行的技能按键。</param>
        private void AddLocalSkillInput(KeyCode key)
        {
            if (key == KeyCode.None)
            {
                return;
            }

            InputObj inputObj = InputObj.Create(key);
            inputObj.PlayerId = GF.UserId;
            _inputObjs.Add(inputObj);
        }

        /// <summary>
        /// 执行当前逻辑帧收集的全部输入，并在执行后归还对象池对象。
        /// </summary>
        private void ExecutePendingInputs()
        {
            if (_unitProxy == null)
            {
                Log.Error("InputProxy 缺少 UnitProxy，已丢弃当前帧输入。");
                ReleaseInputObjects(_inputObjs);
                return;
            }

            try
            {
                foreach (InputObj inputObj in _inputObjs)
                {
                    _unitProxy.OperatorMainRole(inputObj);
                }
            }
            finally
            {
                ReleaseInputObjects(_inputObjs);
            }
        }

        /// <summary>
        /// 归还输入列表中的所有对象池对象并清空列表。
        /// </summary>
        /// <param name="inputObjs">由本代理持有的输入对象列表；允许为空。</param>
        private static void ReleaseInputObjects(List<InputObj> inputObjs)
        {
            if (inputObjs == null)
            {
                return;
            }

            foreach (InputObj inputObj in inputObjs)
            {
                if (inputObj != null)
                {
                    ReferencePool.Release(inputObj);
                }
            }

            inputObjs.Clear();
        }

        /// <summary>
        /// 释放本代理拥有的输入对象，并恢复默认输入状态。
        /// </summary>
        private void ResetInputState()
        {
            _dialogueInputBlocked = false;
            ReleaseInputObjects(_inputObjs);
            _pendingSkillKeys?.Clear();
            _hasPreparedFrameInput = false;
            _roomHorizontal = fix.Zero;
            _roomVertical = fix.Zero;
            _roomInputBlocked = false;
        }

        /// <summary>
        /// 清理缓存输入、代理引用和对象池所有权。
        /// </summary>
        public override void Clear()
        {
            ResetInputState();
            _unitProxy = null;
            base.Clear();
        }
    }
}
