using LightProto;
using System;
using MemoryPack;
using System.Collections.Generic;
using Fantasy;
using Fantasy.Pool;
using Fantasy.Network.Interface;
using Fantasy.Serialize;

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618
#pragma warning disable CS8601 // Possible null reference assignment in generated message copies.
// ReSharper disable InconsistentNaming
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable RedundantTypeArgumentsOfMethod
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable PreferConcreteValueOverDefault
// ReSharper disable RedundantNameQualifier
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable CheckNamespace
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable RedundantUsingDirective
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
namespace Fantasy
{
    /// <summary>网络协议消息 C2G_TestMessage。</summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_TestMessage : AMessage, IMessage
    {
        /// <summary>从消息池创建协议对象。</summary>
        /// <param name="autoReturn">是否允许对象自动归还消息池。</param>
        public static C2G_TestMessage Create(bool autoReturn = true)
        {
            var c2G_TestMessage = MessageObjectPool<C2G_TestMessage>.Rent();
            c2G_TestMessage.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2G_TestMessage.SetIsPool(false);
            }
            
            return c2G_TestMessage;
        }
        
        /// <summary>归还当前协议对象。</summary>
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        /// <summary>清空字段并归还消息池。</summary>
        public void Dispose()
        {
            if (!IsPool()) return; 
            Tag = default;
            MessageObjectPool<C2G_TestMessage>.Return(this);
        }

        /// <summary>复制协议字段到目标消息。</summary>
        /// <param name="other">接收副本字段的目标消息。</param>
        public void CopyTo(ref C2G_TestMessage other)
        {
            if (other == null)
            {
                other = Create(false);
            }
            other.Tag = Tag;

        }

        /// <summary>创建独立的消息副本。</summary>
        public C2G_TestMessage DeepCopy()
        {
            var other = Create(false);
            CopyTo(ref other);
            return other;
        }
        /// <summary>取得本消息的协议编号。</summary>
        public uint OpCode() { return OuterOpcode.C2G_TestMessage; } 
        /// <summary>协议字段 Tag。</summary>
        [ProtoMember(1)]
        public string Tag { get; set; }
    }
    /// <summary>网络协议消息 C2G_TestRequest。</summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_TestRequest : AMessage, IRequest
    {
        /// <summary>从消息池创建协议对象。</summary>
        /// <param name="autoReturn">是否允许对象自动归还消息池。</param>
        public static C2G_TestRequest Create(bool autoReturn = true)
        {
            var c2G_TestRequest = MessageObjectPool<C2G_TestRequest>.Rent();
            c2G_TestRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2G_TestRequest.SetIsPool(false);
            }
            
            return c2G_TestRequest;
        }
        
        /// <summary>归还当前协议对象。</summary>
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        /// <summary>清空字段并归还消息池。</summary>
        public void Dispose()
        {
            if (!IsPool()) return; 
            Tag = default;
            MessageObjectPool<C2G_TestRequest>.Return(this);
        }

        /// <summary>复制协议字段到目标消息。</summary>
        /// <param name="other">接收副本字段的目标消息。</param>
        public void CopyTo(ref C2G_TestRequest other)
        {
            if (other == null)
            {
                other = Create(false);
            }
            other.Tag = Tag;

        }

        /// <summary>创建独立的消息副本。</summary>
        public C2G_TestRequest DeepCopy()
        {
            var other = Create(false);
            CopyTo(ref other);
            return other;
        }
        /// <summary>取得本消息的协议编号。</summary>
        public uint OpCode() { return OuterOpcode.C2G_TestRequest; } 
        /// <summary>请求对应的响应类型，仅供框架推断。</summary>
        [ProtoIgnore]
        public G2C_TestResponse ResponseType { get; set; }
        /// <summary>协议字段 Tag。</summary>
        [ProtoMember(1)]
        public string Tag { get; set; }
    }
    /// <summary>网络协议消息 G2C_TestResponse。</summary>
    [Serializable]
    [ProtoContract]
    public partial class G2C_TestResponse : AMessage, IResponse
    {
        /// <summary>从消息池创建协议对象。</summary>
        /// <param name="autoReturn">是否允许对象自动归还消息池。</param>
        public static G2C_TestResponse Create(bool autoReturn = true)
        {
            var g2C_TestResponse = MessageObjectPool<G2C_TestResponse>.Rent();
            g2C_TestResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2C_TestResponse.SetIsPool(false);
            }
            
            return g2C_TestResponse;
        }
        
        /// <summary>归还当前协议对象。</summary>
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        /// <summary>清空字段并归还消息池。</summary>
        public void Dispose()
        {
            if (!IsPool()) return; 
            ErrorCode = 0;
            Tag = default;
            MessageObjectPool<G2C_TestResponse>.Return(this);
        }

        /// <summary>复制协议字段到目标消息。</summary>
        /// <param name="other">接收副本字段的目标消息。</param>
        public void CopyTo(ref G2C_TestResponse other)
        {
            if (other == null)
            {
                other = Create(false);
            }
            other.ErrorCode = ErrorCode;
            other.Tag = Tag;

        }

        /// <summary>创建独立的消息副本。</summary>
        public G2C_TestResponse DeepCopy()
        {
            var other = Create(false);
            CopyTo(ref other);
            return other;
        }
        /// <summary>取得本消息的协议编号。</summary>
        public uint OpCode() { return OuterOpcode.G2C_TestResponse; } 
        /// <summary>操作结果，零表示成功。</summary>
        [ProtoMember(2)]
        public uint ErrorCode { get; set; }
        /// <summary>协议字段 Tag。</summary>
        [ProtoMember(1)]
        public string Tag { get; set; }
    }
    /// <summary>使用 HTTP 认证签发的一次性票据登录游戏服。</summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_LoginRequest : AMessage, IRequest
    {
        /// <summary>从消息池创建协议对象。</summary>
        /// <param name="autoReturn">是否允许对象自动归还消息池。</param>
        public static C2G_LoginRequest Create(bool autoReturn = true)
        {
            var c2G_LoginRequest = MessageObjectPool<C2G_LoginRequest>.Rent();
            c2G_LoginRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2G_LoginRequest.SetIsPool(false);
            }
            
            return c2G_LoginRequest;
        }
        
        /// <summary>归还当前协议对象。</summary>
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        /// <summary>清空字段并归还消息池。</summary>
        public void Dispose()
        {
            if (!IsPool()) return; 
            Id = default;
            Ticket = default;
            MessageObjectPool<C2G_LoginRequest>.Return(this);
        }

        /// <summary>复制协议字段到目标消息。</summary>
        /// <param name="other">接收副本字段的目标消息。</param>
        public void CopyTo(ref C2G_LoginRequest other)
        {
            if (other == null)
            {
                other = Create(false);
            }
            other.Id = Id;
            other.Ticket = Ticket;

        }

        /// <summary>创建独立的消息副本。</summary>
        public C2G_LoginRequest DeepCopy()
        {
            var other = Create(false);
            CopyTo(ref other);
            return other;
        }
        /// <summary>取得本消息的协议编号。</summary>
        public uint OpCode() { return OuterOpcode.C2G_LoginRequest; } 
        /// <summary>请求对应的响应类型，仅供框架推断。</summary>
        [ProtoIgnore]
        public G2C_LoginResponse ResponseType { get; set; }
        /// <summary>废弃字段，仅保留原编号；服务端忽略客户端声称的玩家编号。</summary>
        [ProtoMember(1)]
        public int Id { get; set; }
        /// <summary>绑定目标 Gate、玩家和当前登录授权的一次性票据。</summary>
        [ProtoMember(2)]
        public string Ticket { get; set; }
    }
    /// <summary>游戏服确认的认证结果和玩家资料。</summary>
    [Serializable]
    [ProtoContract]
    public partial class G2C_LoginResponse : AMessage, IResponse
    {
        /// <summary>从消息池创建协议对象。</summary>
        /// <param name="autoReturn">是否允许对象自动归还消息池。</param>
        public static G2C_LoginResponse Create(bool autoReturn = true)
        {
            var g2C_LoginResponse = MessageObjectPool<G2C_LoginResponse>.Rent();
            g2C_LoginResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2C_LoginResponse.SetIsPool(false);
            }
            
            return g2C_LoginResponse;
        }
        
        /// <summary>归还当前协议对象。</summary>
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        /// <summary>清空字段并归还消息池。</summary>
        public void Dispose()
        {
            if (!IsPool()) return; 
            ErrorCode = 0;
            Tag = default;
            PlayerId = default;
            PlayerName = default;
            MessageObjectPool<G2C_LoginResponse>.Return(this);
        }

        /// <summary>复制协议字段到目标消息。</summary>
        /// <param name="other">接收副本字段的目标消息。</param>
        public void CopyTo(ref G2C_LoginResponse other)
        {
            if (other == null)
            {
                other = Create(false);
            }
            other.ErrorCode = ErrorCode;
            other.Tag = Tag;
            other.PlayerId = PlayerId;
            other.PlayerName = PlayerName;

        }

        /// <summary>创建独立的消息副本。</summary>
        public G2C_LoginResponse DeepCopy()
        {
            var other = Create(false);
            CopyTo(ref other);
            return other;
        }
        /// <summary>取得本消息的协议编号。</summary>
        public uint OpCode() { return OuterOpcode.G2C_LoginResponse; } 
        /// <summary>操作结果，零表示成功。</summary>
        [ProtoMember(2)]
        public uint ErrorCode { get; set; }
        /// <summary>面向玩家的登录结果说明。</summary>
        [ProtoMember(1)]
        public string Tag { get; set; }
        /// <summary>服务端确认的玩家编号；字段 2 留给工具生成的 ErrorCode。</summary>
        [ProtoMember(3)]
        public int PlayerId { get; set; }
        /// <summary>玩家显示名称。</summary>
        [ProtoMember(4)]
        public string PlayerName { get; set; }
    }
    /// <summary>当前会话已被另一次登录替换，客户端应停止重连并返回登录页。</summary>
    [Serializable]
    [ProtoContract]
    public partial class G2C_LoginRevoked : AMessage, IMessage
    {
        /// <summary>从消息池创建协议对象。</summary>
        /// <param name="autoReturn">是否允许对象自动归还消息池。</param>
        public static G2C_LoginRevoked Create(bool autoReturn = true)
        {
            var g2C_LoginRevoked = MessageObjectPool<G2C_LoginRevoked>.Rent();
            g2C_LoginRevoked.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2C_LoginRevoked.SetIsPool(false);
            }
            
            return g2C_LoginRevoked;
        }
        
        /// <summary>归还当前协议对象。</summary>
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        /// <summary>清空字段并归还消息池。</summary>
        public void Dispose()
        {
            if (!IsPool()) return; 
            Reason = default;
            Message = default;
            MessageObjectPool<G2C_LoginRevoked>.Return(this);
        }

        /// <summary>复制协议字段到目标消息。</summary>
        /// <param name="other">接收副本字段的目标消息。</param>
        public void CopyTo(ref G2C_LoginRevoked other)
        {
            if (other == null)
            {
                other = Create(false);
            }
            other.Reason = Reason;
            other.Message = Message;

        }

        /// <summary>创建独立的消息副本。</summary>
        public G2C_LoginRevoked DeepCopy()
        {
            var other = Create(false);
            CopyTo(ref other);
            return other;
        }
        /// <summary>取得本消息的协议编号。</summary>
        public uint OpCode() { return OuterOpcode.G2C_LoginRevoked; } 
        /// <summary>认证业务错误码。</summary>
        [ProtoMember(1)]
        public uint Reason { get; set; }
        /// <summary>可向玩家展示的退出原因。</summary>
        [ProtoMember(2)]
        public string Message { get; set; }
    }
    /// <summary>网络协议消息 FrameCommand。</summary>
    [Serializable]
    [ProtoContract]
    public partial class FrameCommand : AMessage, IDisposable
    {
        /// <summary>从消息池创建协议对象。</summary>
        /// <param name="autoReturn">是否允许对象自动归还消息池。</param>
        public static FrameCommand Create(bool autoReturn = true)
        {
            var frameCommand = MessageObjectPool<FrameCommand>.Rent();
            frameCommand.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                frameCommand.SetIsPool(false);
            }
            
            return frameCommand;
        }
        
        /// <summary>归还当前协议对象。</summary>
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        /// <summary>清空字段并归还消息池。</summary>
        public void Dispose()
        {
            if (!IsPool()) return; 
            Uid = default;
            CommandType = default;
            x = default;
            y = default;
            z = default;
            MessageObjectPool<FrameCommand>.Return(this);
        }

        /// <summary>复制协议字段到目标消息。</summary>
        /// <param name="other">接收副本字段的目标消息。</param>
        public void CopyTo(ref FrameCommand other)
        {
            if (other == null)
            {
                other = Create(false);
            }
            other.Uid = Uid;
            other.CommandType = CommandType;
            other.x = x;
            other.y = y;
            other.z = z;

        }

        /// <summary>创建独立的消息副本。</summary>
        public FrameCommand DeepCopy()
        {
            var other = Create(false);
            CopyTo(ref other);
            return other;
        }
        /// <summary>协议字段 Uid。</summary>
        [ProtoMember(1)]
        public int Uid { get; set; }
        /// <summary>协议字段 CommandType。</summary>
        [ProtoMember(2)]
        public int CommandType { get; set; }
        /// <summary>协议字段 x。</summary>
        [ProtoMember(3)]
        public int x { get; set; }
        /// <summary>协议字段 y。</summary>
        [ProtoMember(4)]
        public int y { get; set; }
        /// <summary>协议字段 z。</summary>
        [ProtoMember(5)]
        public int z { get; set; }
    }
    /// <summary>网络协议消息 OneFrameCommand。</summary>
    [Serializable]
    [ProtoContract]
    public partial class OneFrameCommand : AMessage, IDisposable
    {
        /// <summary>从消息池创建协议对象。</summary>
        /// <param name="autoReturn">是否允许对象自动归还消息池。</param>
        public static OneFrameCommand Create(bool autoReturn = true)
        {
            var oneFrameCommand = MessageObjectPool<OneFrameCommand>.Rent();
            oneFrameCommand.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                oneFrameCommand.SetIsPool(false);
            }
            
            return oneFrameCommand;
        }
        
        /// <summary>归还当前协议对象。</summary>
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        /// <summary>清空字段并归还消息池。</summary>
        public void Dispose()
        {
            if (!IsPool()) return; 
            FrameId = default;
            foreach (var __t in UnitFrameCommand) __t.Dispose();
            UnitFrameCommand.Clear();
            MessageObjectPool<OneFrameCommand>.Return(this);
        }

        /// <summary>复制协议字段到目标消息。</summary>
        /// <param name="other">接收副本字段的目标消息。</param>
        public void CopyTo(ref OneFrameCommand other)
        {
            if (other == null)
            {
                other = Create(false);
            }
            other.FrameId = FrameId;
            other.UnitFrameCommand = new List<FrameCommand>(UnitFrameCommand.Count);
            foreach (var __item in UnitFrameCommand) other.UnitFrameCommand.Add(__item.DeepCopy());

        }

        /// <summary>创建独立的消息副本。</summary>
        public OneFrameCommand DeepCopy()
        {
            var other = Create(false);
            CopyTo(ref other);
            return other;
        }
        /// <summary>协议字段 FrameId。</summary>
        [ProtoMember(1)]
        public int FrameId { get; set; }
        /// <summary>协议字段 UnitFrameCommand。</summary>
        [ProtoMember(2)]
        public List<FrameCommand> UnitFrameCommand { get; set; } = new List<FrameCommand>();
    }
    /// <summary>网络协议消息 G2C_FrameCommand。</summary>
    [Serializable]
    [ProtoContract]
    public partial class G2C_FrameCommand : AMessage, IMessage
    {
        /// <summary>从消息池创建协议对象。</summary>
        /// <param name="autoReturn">是否允许对象自动归还消息池。</param>
        public static G2C_FrameCommand Create(bool autoReturn = true)
        {
            var g2C_FrameCommand = MessageObjectPool<G2C_FrameCommand>.Rent();
            g2C_FrameCommand.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2C_FrameCommand.SetIsPool(false);
            }
            
            return g2C_FrameCommand;
        }
        
        /// <summary>归还当前协议对象。</summary>
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        /// <summary>清空字段并归还消息池。</summary>
        public void Dispose()
        {
            if (!IsPool()) return; 
            FrameId = default;
            foreach (var __t in Commands) __t.Dispose();
            Commands.Clear();
            MessageObjectPool<G2C_FrameCommand>.Return(this);
        }

        /// <summary>复制协议字段到目标消息。</summary>
        /// <param name="other">接收副本字段的目标消息。</param>
        public void CopyTo(ref G2C_FrameCommand other)
        {
            if (other == null)
            {
                other = Create(false);
            }
            other.FrameId = FrameId;
            other.Commands = new List<OneFrameCommand>(Commands.Count);
            foreach (var __item in Commands) other.Commands.Add(__item.DeepCopy());

        }

        /// <summary>创建独立的消息副本。</summary>
        public G2C_FrameCommand DeepCopy()
        {
            var other = Create(false);
            CopyTo(ref other);
            return other;
        }
        /// <summary>取得本消息的协议编号。</summary>
        public uint OpCode() { return OuterOpcode.G2C_FrameCommand; } 
        /// <summary>协议字段 FrameId。</summary>
        [ProtoMember(1)]
        public int FrameId { get; set; }
        /// <summary>协议字段 Commands。</summary>
        [ProtoMember(2)]
        public List<OneFrameCommand> Commands { get; set; } = new List<OneFrameCommand>();
    }
    /// <summary>网络协议消息 C2G_NextFrameCommand。</summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_NextFrameCommand : AMessage, IMessage
    {
        /// <summary>从消息池创建协议对象。</summary>
        /// <param name="autoReturn">是否允许对象自动归还消息池。</param>
        public static C2G_NextFrameCommand Create(bool autoReturn = true)
        {
            var c2G_NextFrameCommand = MessageObjectPool<C2G_NextFrameCommand>.Rent();
            c2G_NextFrameCommand.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2G_NextFrameCommand.SetIsPool(false);
            }
            
            return c2G_NextFrameCommand;
        }
        
        /// <summary>归还当前协议对象。</summary>
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        /// <summary>清空字段并归还消息池。</summary>
        public void Dispose()
        {
            if (!IsPool()) return; 
            AckFrameId = default;
            foreach (var __t in Commands) __t.Dispose();
            Commands.Clear();
            InputSequence = default;
            MessageObjectPool<C2G_NextFrameCommand>.Return(this);
        }

        /// <summary>复制协议字段到目标消息。</summary>
        /// <param name="other">接收副本字段的目标消息。</param>
        public void CopyTo(ref C2G_NextFrameCommand other)
        {
            if (other == null)
            {
                other = Create(false);
            }
            other.AckFrameId = AckFrameId;
            other.Commands = new List<FrameCommand>(Commands.Count);
            foreach (var __item in Commands) other.Commands.Add(__item.DeepCopy());
            other.InputSequence = InputSequence;

        }

        /// <summary>创建独立的消息副本。</summary>
        public C2G_NextFrameCommand DeepCopy()
        {
            var other = Create(false);
            CopyTo(ref other);
            return other;
        }
        /// <summary>取得本消息的协议编号。</summary>
        public uint OpCode() { return OuterOpcode.C2G_NextFrameCommand; } 
        /// <summary>客户端已完成模拟的连续服务端帧号。</summary>
        [ProtoMember(1)]
        public int AckFrameId { get; set; }
        /// <summary>协议字段 Commands。</summary>
        [ProtoMember(2)]
        public List<FrameCommand> Commands { get; set; } = new List<FrameCommand>();
        /// <summary>每个连接内单调递增，用于将输入去重与帧确认解耦。</summary>
        [ProtoMember(3)]
        public int InputSequence { get; set; }
    }
    /// <summary>网络协议消息 C2G_FrameAck。</summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_FrameAck : AMessage, IMessage
    {
        /// <summary>从消息池创建协议对象。</summary>
        /// <param name="autoReturn">是否允许对象自动归还消息池。</param>
        public static C2G_FrameAck Create(bool autoReturn = true)
        {
            var c2G_FrameAck = MessageObjectPool<C2G_FrameAck>.Rent();
            c2G_FrameAck.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2G_FrameAck.SetIsPool(false);
            }
            
            return c2G_FrameAck;
        }
        
        /// <summary>归还当前协议对象。</summary>
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        /// <summary>清空字段并归还消息池。</summary>
        public void Dispose()
        {
            if (!IsPool()) return; 
            AckFrameId = default;
            MessageObjectPool<C2G_FrameAck>.Return(this);
        }

        /// <summary>复制协议字段到目标消息。</summary>
        /// <param name="other">接收副本字段的目标消息。</param>
        public void CopyTo(ref C2G_FrameAck other)
        {
            if (other == null)
            {
                other = Create(false);
            }
            other.AckFrameId = AckFrameId;

        }

        /// <summary>创建独立的消息副本。</summary>
        public C2G_FrameAck DeepCopy()
        {
            var other = Create(false);
            CopyTo(ref other);
            return other;
        }
        /// <summary>取得本消息的协议编号。</summary>
        public uint OpCode() { return OuterOpcode.C2G_FrameAck; } 
        /// <summary>客户端已完成模拟的连续服务端帧号。</summary>
        [ProtoMember(1)]
        public int AckFrameId { get; set; }
    }
    /// <summary>网络协议消息 C2G_FrameFetchRequest。</summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_FrameFetchRequest : AMessage, IMessage
    {
        /// <summary>从消息池创建协议对象。</summary>
        /// <param name="autoReturn">是否允许对象自动归还消息池。</param>
        public static C2G_FrameFetchRequest Create(bool autoReturn = true)
        {
            var c2G_FrameFetchRequest = MessageObjectPool<C2G_FrameFetchRequest>.Rent();
            c2G_FrameFetchRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2G_FrameFetchRequest.SetIsPool(false);
            }
            
            return c2G_FrameFetchRequest;
        }
        
        /// <summary>归还当前协议对象。</summary>
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        /// <summary>清空字段并归还消息池。</summary>
        public void Dispose()
        {
            if (!IsPool()) return; 
            MatchId = default;
            StartFrameId = default;
            MessageObjectPool<C2G_FrameFetchRequest>.Return(this);
        }

        /// <summary>复制协议字段到目标消息。</summary>
        /// <param name="other">接收副本字段的目标消息。</param>
        public void CopyTo(ref C2G_FrameFetchRequest other)
        {
            if (other == null)
            {
                other = Create(false);
            }
            other.MatchId = MatchId;
            other.StartFrameId = StartFrameId;

        }

        /// <summary>创建独立的消息副本。</summary>
        public C2G_FrameFetchRequest DeepCopy()
        {
            var other = Create(false);
            CopyTo(ref other);
            return other;
        }
        /// <summary>取得本消息的协议编号。</summary>
        public uint OpCode() { return OuterOpcode.C2G_FrameFetchRequest; } 
        /// <summary>调用方当前所属的对局标识，用于拒绝过期房间请求。</summary>
        [ProtoMember(1)]
        public int MatchId { get; set; }
        /// <summary>需要补取的第一个连续逻辑帧号。</summary>
        [ProtoMember(2)]
        public int StartFrameId { get; set; }
    }
    /// <summary>网络协议消息 C2G_CreateFrameRoom。</summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_CreateFrameRoom : AMessage, IRequest
    {
        /// <summary>从消息池创建协议对象。</summary>
        /// <param name="autoReturn">是否允许对象自动归还消息池。</param>
        public static C2G_CreateFrameRoom Create(bool autoReturn = true)
        {
            var c2G_CreateFrameRoom = MessageObjectPool<C2G_CreateFrameRoom>.Rent();
            c2G_CreateFrameRoom.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2G_CreateFrameRoom.SetIsPool(false);
            }
            
            return c2G_CreateFrameRoom;
        }
        
        /// <summary>归还当前协议对象。</summary>
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        /// <summary>清空字段并归还消息池。</summary>
        public void Dispose()
        {
            if (!IsPool()) return; 
            DungeonId = default;
            DungeonType = default;
            HeroId = default;
            MessageObjectPool<C2G_CreateFrameRoom>.Return(this);
        }

        /// <summary>复制协议字段到目标消息。</summary>
        /// <param name="other">接收副本字段的目标消息。</param>
        public void CopyTo(ref C2G_CreateFrameRoom other)
        {
            if (other == null)
            {
                other = Create(false);
            }
            other.DungeonId = DungeonId;
            other.DungeonType = DungeonType;
            other.HeroId = HeroId;

        }

        /// <summary>创建独立的消息副本。</summary>
        public C2G_CreateFrameRoom DeepCopy()
        {
            var other = Create(false);
            CopyTo(ref other);
            return other;
        }
        /// <summary>取得本消息的协议编号。</summary>
        public uint OpCode() { return OuterOpcode.C2G_CreateFrameRoom; } 
        /// <summary>请求对应的响应类型，仅供框架推断。</summary>
        [ProtoIgnore]
        public G2C_CreateFrameRoom ResponseType { get; set; }
        /// <summary>创建房间使用的副本配置标识。</summary>
        [ProtoMember(1)]
        public int DungeonId { get; set; }
        /// <summary>创建房间使用的副本类型。</summary>
        [ProtoMember(2)]
        public int DungeonType { get; set; }
        /// <summary>创建者选择的英雄配置标识。</summary>
        [ProtoMember(3)]
        public int HeroId { get; set; }
    }
    /// <summary>网络协议消息 G2C_CreateFrameRoom。</summary>
    [Serializable]
    [ProtoContract]
    public partial class G2C_CreateFrameRoom : AMessage, IResponse
    {
        /// <summary>从消息池创建协议对象。</summary>
        /// <param name="autoReturn">是否允许对象自动归还消息池。</param>
        public static G2C_CreateFrameRoom Create(bool autoReturn = true)
        {
            var g2C_CreateFrameRoom = MessageObjectPool<G2C_CreateFrameRoom>.Rent();
            g2C_CreateFrameRoom.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2C_CreateFrameRoom.SetIsPool(false);
            }
            
            return g2C_CreateFrameRoom;
        }
        
        /// <summary>归还当前协议对象。</summary>
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        /// <summary>清空字段并归还消息池。</summary>
        public void Dispose()
        {
            if (!IsPool()) return; 
            ErrorCode = 0;
            ErrorCode = default;
            ErrorMessage = default;
            RoomId = default;
            MessageObjectPool<G2C_CreateFrameRoom>.Return(this);
        }

        /// <summary>复制协议字段到目标消息。</summary>
        /// <param name="other">接收副本字段的目标消息。</param>
        public void CopyTo(ref G2C_CreateFrameRoom other)
        {
            if (other == null)
            {
                other = Create(false);
            }
            other.ErrorCode = ErrorCode;
            other.ErrorMessage = ErrorMessage;
            other.RoomId = RoomId;

        }

        /// <summary>创建独立的消息副本。</summary>
        public G2C_CreateFrameRoom DeepCopy()
        {
            var other = Create(false);
            CopyTo(ref other);
            return other;
        }
        /// <summary>取得本消息的协议编号。</summary>
        public uint OpCode() { return OuterOpcode.G2C_CreateFrameRoom; } 
        /// <summary>操作结果，零表示成功。</summary>
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        /// <summary>失败时返回的可定位原因。</summary>
        [ProtoMember(2)]
        public string ErrorMessage { get; set; }
        /// <summary>成功创建后的双人房间标识。</summary>
        [ProtoMember(3)]
        public int RoomId { get; set; }
    }
    /// <summary>网络协议消息 C2G_JoinFrameRoom。</summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_JoinFrameRoom : AMessage, IRequest
    {
        /// <summary>从消息池创建协议对象。</summary>
        /// <param name="autoReturn">是否允许对象自动归还消息池。</param>
        public static C2G_JoinFrameRoom Create(bool autoReturn = true)
        {
            var c2G_JoinFrameRoom = MessageObjectPool<C2G_JoinFrameRoom>.Rent();
            c2G_JoinFrameRoom.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2G_JoinFrameRoom.SetIsPool(false);
            }
            
            return c2G_JoinFrameRoom;
        }
        
        /// <summary>归还当前协议对象。</summary>
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        /// <summary>清空字段并归还消息池。</summary>
        public void Dispose()
        {
            if (!IsPool()) return; 
            RoomId = default;
            HeroId = default;
            MessageObjectPool<C2G_JoinFrameRoom>.Return(this);
        }

        /// <summary>复制协议字段到目标消息。</summary>
        /// <param name="other">接收副本字段的目标消息。</param>
        public void CopyTo(ref C2G_JoinFrameRoom other)
        {
            if (other == null)
            {
                other = Create(false);
            }
            other.RoomId = RoomId;
            other.HeroId = HeroId;

        }

        /// <summary>创建独立的消息副本。</summary>
        public C2G_JoinFrameRoom DeepCopy()
        {
            var other = Create(false);
            CopyTo(ref other);
            return other;
        }
        /// <summary>取得本消息的协议编号。</summary>
        public uint OpCode() { return OuterOpcode.C2G_JoinFrameRoom; } 
        /// <summary>请求对应的响应类型，仅供框架推断。</summary>
        [ProtoIgnore]
        public G2C_JoinFrameRoom ResponseType { get; set; }
        /// <summary>要加入的双人房间标识。</summary>
        [ProtoMember(1)]
        public int RoomId { get; set; }
        /// <summary>加入者选择的英雄配置标识。</summary>
        [ProtoMember(2)]
        public int HeroId { get; set; }
    }
    /// <summary>网络协议消息 G2C_JoinFrameRoom。</summary>
    [Serializable]
    [ProtoContract]
    public partial class G2C_JoinFrameRoom : AMessage, IResponse
    {
        /// <summary>从消息池创建协议对象。</summary>
        /// <param name="autoReturn">是否允许对象自动归还消息池。</param>
        public static G2C_JoinFrameRoom Create(bool autoReturn = true)
        {
            var g2C_JoinFrameRoom = MessageObjectPool<G2C_JoinFrameRoom>.Rent();
            g2C_JoinFrameRoom.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2C_JoinFrameRoom.SetIsPool(false);
            }
            
            return g2C_JoinFrameRoom;
        }
        
        /// <summary>归还当前协议对象。</summary>
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        /// <summary>清空字段并归还消息池。</summary>
        public void Dispose()
        {
            if (!IsPool()) return; 
            ErrorCode = 0;
            ErrorCode = default;
            ErrorMessage = default;
            RoomId = default;
            MessageObjectPool<G2C_JoinFrameRoom>.Return(this);
        }

        /// <summary>复制协议字段到目标消息。</summary>
        /// <param name="other">接收副本字段的目标消息。</param>
        public void CopyTo(ref G2C_JoinFrameRoom other)
        {
            if (other == null)
            {
                other = Create(false);
            }
            other.ErrorCode = ErrorCode;
            other.ErrorMessage = ErrorMessage;
            other.RoomId = RoomId;

        }

        /// <summary>创建独立的消息副本。</summary>
        public G2C_JoinFrameRoom DeepCopy()
        {
            var other = Create(false);
            CopyTo(ref other);
            return other;
        }
        /// <summary>取得本消息的协议编号。</summary>
        public uint OpCode() { return OuterOpcode.G2C_JoinFrameRoom; } 
        /// <summary>操作结果，零表示成功。</summary>
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        /// <summary>失败时返回的可定位原因。</summary>
        [ProtoMember(2)]
        public string ErrorMessage { get; set; }
        /// <summary>成功加入的房间标识。</summary>
        [ProtoMember(3)]
        public int RoomId { get; set; }
    }
    /// <summary>网络协议消息 FrameMatchPlayer。</summary>
    [Serializable]
    [ProtoContract]
    public partial class FrameMatchPlayer : AMessage, IDisposable
    {
        /// <summary>从消息池创建协议对象。</summary>
        /// <param name="autoReturn">是否允许对象自动归还消息池。</param>
        public static FrameMatchPlayer Create(bool autoReturn = true)
        {
            var frameMatchPlayer = MessageObjectPool<FrameMatchPlayer>.Rent();
            frameMatchPlayer.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                frameMatchPlayer.SetIsPool(false);
            }
            
            return frameMatchPlayer;
        }
        
        /// <summary>归还当前协议对象。</summary>
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        /// <summary>清空字段并归还消息池。</summary>
        public void Dispose()
        {
            if (!IsPool()) return; 
            PlayerId = default;
            HeroIds.Clear();
            Position = default;
            MessageObjectPool<FrameMatchPlayer>.Return(this);
        }

        /// <summary>复制协议字段到目标消息。</summary>
        /// <param name="other">接收副本字段的目标消息。</param>
        public void CopyTo(ref FrameMatchPlayer other)
        {
            if (other == null)
            {
                other = Create(false);
            }
            other.PlayerId = PlayerId;
            other.HeroIds = new List<int>(HeroIds);
            other.Position = Position;

        }

        /// <summary>创建独立的消息副本。</summary>
        public FrameMatchPlayer DeepCopy()
        {
            var other = Create(false);
            CopyTo(ref other);
            return other;
        }
        /// <summary>协议字段 PlayerId。</summary>
        [ProtoMember(1)]
        public int PlayerId { get; set; }
        /// <summary>协议字段 HeroIds。</summary>
        [ProtoMember(2)]
        public List<int> HeroIds { get; set; } = new List<int>();
        /// <summary>协议字段 Position。</summary>
        [ProtoMember(3)]
        public int Position { get; set; }
    }
    /// <summary>网络协议消息 G2C_FrameMatchBootstrap。</summary>
    [Serializable]
    [ProtoContract]
    public partial class G2C_FrameMatchBootstrap : AMessage, IMessage
    {
        /// <summary>从消息池创建协议对象。</summary>
        /// <param name="autoReturn">是否允许对象自动归还消息池。</param>
        public static G2C_FrameMatchBootstrap Create(bool autoReturn = true)
        {
            var g2C_FrameMatchBootstrap = MessageObjectPool<G2C_FrameMatchBootstrap>.Rent();
            g2C_FrameMatchBootstrap.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2C_FrameMatchBootstrap.SetIsPool(false);
            }
            
            return g2C_FrameMatchBootstrap;
        }
        
        /// <summary>归还当前协议对象。</summary>
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        /// <summary>清空字段并归还消息池。</summary>
        public void Dispose()
        {
            if (!IsPool()) return; 
            MatchId = default;
            RandomSeed = default;
            DungeonId = default;
            DungeonType = default;
            CurrentFrameId = default;
            foreach (var __t in Players) __t.Dispose();
            Players.Clear();
            MessageObjectPool<G2C_FrameMatchBootstrap>.Return(this);
        }

        /// <summary>复制协议字段到目标消息。</summary>
        /// <param name="other">接收副本字段的目标消息。</param>
        public void CopyTo(ref G2C_FrameMatchBootstrap other)
        {
            if (other == null)
            {
                other = Create(false);
            }
            other.MatchId = MatchId;
            other.RandomSeed = RandomSeed;
            other.DungeonId = DungeonId;
            other.DungeonType = DungeonType;
            other.CurrentFrameId = CurrentFrameId;
            other.Players = new List<FrameMatchPlayer>(Players.Count);
            foreach (var __item in Players) other.Players.Add(__item.DeepCopy());

        }

        /// <summary>创建独立的消息副本。</summary>
        public G2C_FrameMatchBootstrap DeepCopy()
        {
            var other = Create(false);
            CopyTo(ref other);
            return other;
        }
        /// <summary>取得本消息的协议编号。</summary>
        public uint OpCode() { return OuterOpcode.G2C_FrameMatchBootstrap; } 
        /// <summary>协议字段 MatchId。</summary>
        [ProtoMember(1)]
        public int MatchId { get; set; }
        /// <summary>协议字段 RandomSeed。</summary>
        [ProtoMember(2)]
        public int RandomSeed { get; set; }
        /// <summary>协议字段 DungeonId。</summary>
        [ProtoMember(3)]
        public int DungeonId { get; set; }
        /// <summary>协议字段 DungeonType。</summary>
        [ProtoMember(4)]
        public int DungeonType { get; set; }
        /// <summary>协议字段 CurrentFrameId。</summary>
        [ProtoMember(5)]
        public int CurrentFrameId { get; set; }
        /// <summary>协议字段 Players。</summary>
        [ProtoMember(6)]
        public List<FrameMatchPlayer> Players { get; set; } = new List<FrameMatchPlayer>();
    }
    /// <summary>账号任务目标进度。任务状态与剧情标记分域保存于同一玩家进度文档。</summary>
    [Serializable]
    [ProtoContract]
    public partial class PlayerObjectiveProgress : AMessage, IDisposable
    {
        /// <summary>从消息池创建协议对象。</summary>
        /// <param name="autoReturn">是否允许对象自动归还消息池。</param>
        public static PlayerObjectiveProgress Create(bool autoReturn = true)
        {
            var playerObjectiveProgress = MessageObjectPool<PlayerObjectiveProgress>.Rent();
            playerObjectiveProgress.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                playerObjectiveProgress.SetIsPool(false);
            }
            
            return playerObjectiveProgress;
        }
        
        /// <summary>归还当前协议对象。</summary>
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        /// <summary>清空字段并归还消息池。</summary>
        public void Dispose()
        {
            if (!IsPool()) return; 
            ObjectiveId = default;
            Count = default;
            MessageObjectPool<PlayerObjectiveProgress>.Return(this);
        }

        /// <summary>复制协议字段到目标消息。</summary>
        /// <param name="other">接收副本字段的目标消息。</param>
        public void CopyTo(ref PlayerObjectiveProgress other)
        {
            if (other == null)
            {
                other = Create(false);
            }
            other.ObjectiveId = ObjectiveId;
            other.Count = Count;

        }

        /// <summary>创建独立的消息副本。</summary>
        public PlayerObjectiveProgress DeepCopy()
        {
            var other = Create(false);
            CopyTo(ref other);
            return other;
        }
        /// <summary>协议字段 ObjectiveId。</summary>
        [ProtoMember(1)]
        public int ObjectiveId { get; set; }
        /// <summary>协议字段 Count。</summary>
        [ProtoMember(2)]
        public int Count { get; set; }
    }
    /// <summary>网络协议消息 PlayerQuestProgress。</summary>
    [Serializable]
    [ProtoContract]
    public partial class PlayerQuestProgress : AMessage, IDisposable
    {
        /// <summary>从消息池创建协议对象。</summary>
        /// <param name="autoReturn">是否允许对象自动归还消息池。</param>
        public static PlayerQuestProgress Create(bool autoReturn = true)
        {
            var playerQuestProgress = MessageObjectPool<PlayerQuestProgress>.Rent();
            playerQuestProgress.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                playerQuestProgress.SetIsPool(false);
            }
            
            return playerQuestProgress;
        }
        
        /// <summary>归还当前协议对象。</summary>
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        /// <summary>清空字段并归还消息池。</summary>
        public void Dispose()
        {
            if (!IsPool()) return; 
            QuestId = default;
            Status = default;
            foreach (var __t in Objectives) __t.Dispose();
            Objectives.Clear();
            BriefingAcknowledged = default;
            MessageObjectPool<PlayerQuestProgress>.Return(this);
        }

        /// <summary>复制协议字段到目标消息。</summary>
        /// <param name="other">接收副本字段的目标消息。</param>
        public void CopyTo(ref PlayerQuestProgress other)
        {
            if (other == null)
            {
                other = Create(false);
            }
            other.QuestId = QuestId;
            other.Status = Status;
            other.Objectives = new List<PlayerObjectiveProgress>(Objectives.Count);
            foreach (var __item in Objectives) other.Objectives.Add(__item.DeepCopy());
            other.BriefingAcknowledged = BriefingAcknowledged;

        }

        /// <summary>创建独立的消息副本。</summary>
        public PlayerQuestProgress DeepCopy()
        {
            var other = Create(false);
            CopyTo(ref other);
            return other;
        }
        /// <summary>协议字段 QuestId。</summary>
        [ProtoMember(1)]
        public int QuestId { get; set; }
        /// <summary>协议字段 Status。</summary>
        [ProtoMember(2)]
        public int Status { get; set; }
        /// <summary>协议字段 Objectives。</summary>
        [ProtoMember(3)]
        public List<PlayerObjectiveProgress> Objectives { get; set; } = new List<PlayerObjectiveProgress>();
        /// <summary>协议字段 BriefingAcknowledged。</summary>
        [ProtoMember(4)]
        public bool BriefingAcknowledged { get; set; }
    }
    /// <summary>网络协议消息 PlayerStoryFlag。</summary>
    [Serializable]
    [ProtoContract]
    public partial class PlayerStoryFlag : AMessage, IDisposable
    {
        /// <summary>从消息池创建协议对象。</summary>
        /// <param name="autoReturn">是否允许对象自动归还消息池。</param>
        public static PlayerStoryFlag Create(bool autoReturn = true)
        {
            var playerStoryFlag = MessageObjectPool<PlayerStoryFlag>.Rent();
            playerStoryFlag.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                playerStoryFlag.SetIsPool(false);
            }
            
            return playerStoryFlag;
        }
        
        /// <summary>归还当前协议对象。</summary>
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        /// <summary>清空字段并归还消息池。</summary>
        public void Dispose()
        {
            if (!IsPool()) return; 
            Key = default;
            Value = default;
            MessageObjectPool<PlayerStoryFlag>.Return(this);
        }

        /// <summary>复制协议字段到目标消息。</summary>
        /// <param name="other">接收副本字段的目标消息。</param>
        public void CopyTo(ref PlayerStoryFlag other)
        {
            if (other == null)
            {
                other = Create(false);
            }
            other.Key = Key;
            other.Value = Value;

        }

        /// <summary>创建独立的消息副本。</summary>
        public PlayerStoryFlag DeepCopy()
        {
            var other = Create(false);
            CopyTo(ref other);
            return other;
        }
        /// <summary>协议字段 Key。</summary>
        [ProtoMember(1)]
        public string Key { get; set; }
        /// <summary>协议字段 Value。</summary>
        [ProtoMember(2)]
        public bool Value { get; set; }
    }
    /// <summary>网络协议消息 PlayerItemStack。</summary>
    [Serializable]
    [ProtoContract]
    public partial class PlayerItemStack : AMessage, IDisposable
    {
        /// <summary>从消息池创建协议对象。</summary>
        /// <param name="autoReturn">是否允许对象自动归还消息池。</param>
        public static PlayerItemStack Create(bool autoReturn = true)
        {
            var playerItemStack = MessageObjectPool<PlayerItemStack>.Rent();
            playerItemStack.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                playerItemStack.SetIsPool(false);
            }
            
            return playerItemStack;
        }
        
        /// <summary>归还当前协议对象。</summary>
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        /// <summary>清空字段并归还消息池。</summary>
        public void Dispose()
        {
            if (!IsPool()) return; 
            ItemId = default;
            Count = default;
            MessageObjectPool<PlayerItemStack>.Return(this);
        }

        /// <summary>复制协议字段到目标消息。</summary>
        /// <param name="other">接收副本字段的目标消息。</param>
        public void CopyTo(ref PlayerItemStack other)
        {
            if (other == null)
            {
                other = Create(false);
            }
            other.ItemId = ItemId;
            other.Count = Count;

        }

        /// <summary>创建独立的消息副本。</summary>
        public PlayerItemStack DeepCopy()
        {
            var other = Create(false);
            CopyTo(ref other);
            return other;
        }
        /// <summary>协议字段 ItemId。</summary>
        [ProtoMember(1)]
        public int ItemId { get; set; }
        /// <summary>协议字段 Count。</summary>
        [ProtoMember(2)]
        public int Count { get; set; }
    }
    /// <summary>从当前认证会话读取账号进度；请求不包含玩家 ID。</summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_LoadPlayerProgress : AMessage, IRequest
    {
        /// <summary>从消息池创建协议对象。</summary>
        /// <param name="autoReturn">是否允许对象自动归还消息池。</param>
        public static C2G_LoadPlayerProgress Create(bool autoReturn = true)
        {
            var c2G_LoadPlayerProgress = MessageObjectPool<C2G_LoadPlayerProgress>.Rent();
            c2G_LoadPlayerProgress.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2G_LoadPlayerProgress.SetIsPool(false);
            }
            
            return c2G_LoadPlayerProgress;
        }
        
        /// <summary>归还当前协议对象。</summary>
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        /// <summary>清空字段并归还消息池。</summary>
        public void Dispose()
        {
            if (!IsPool()) return; 
            MapId = default;
            MessageObjectPool<C2G_LoadPlayerProgress>.Return(this);
        }

        /// <summary>复制协议字段到目标消息。</summary>
        /// <param name="other">接收副本字段的目标消息。</param>
        public void CopyTo(ref C2G_LoadPlayerProgress other)
        {
            if (other == null)
            {
                other = Create(false);
            }
            other.MapId = MapId;

        }

        /// <summary>创建独立的消息副本。</summary>
        public C2G_LoadPlayerProgress DeepCopy()
        {
            var other = Create(false);
            CopyTo(ref other);
            return other;
        }
        /// <summary>取得本消息的协议编号。</summary>
        public uint OpCode() { return OuterOpcode.C2G_LoadPlayerProgress; } 
        /// <summary>请求对应的响应类型，仅供框架推断。</summary>
        [ProtoIgnore]
        public G2C_LoadPlayerProgress ResponseType { get; set; }
        /// <summary>协议字段 MapId。</summary>
        [ProtoMember(1)]
        public int MapId { get; set; }
    }
    /// <summary>网络协议消息 G2C_LoadPlayerProgress。</summary>
    [Serializable]
    [ProtoContract]
    public partial class G2C_LoadPlayerProgress : AMessage, IResponse
    {
        /// <summary>从消息池创建协议对象。</summary>
        /// <param name="autoReturn">是否允许对象自动归还消息池。</param>
        public static G2C_LoadPlayerProgress Create(bool autoReturn = true)
        {
            var g2C_LoadPlayerProgress = MessageObjectPool<G2C_LoadPlayerProgress>.Rent();
            g2C_LoadPlayerProgress.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2C_LoadPlayerProgress.SetIsPool(false);
            }
            
            return g2C_LoadPlayerProgress;
        }
        
        /// <summary>归还当前协议对象。</summary>
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        /// <summary>清空字段并归还消息池。</summary>
        public void Dispose()
        {
            if (!IsPool()) return; 
            ErrorCode = 0;
            ErrorCode = default;
            ErrorMessage = default;
            Revision = default;
            foreach (var __t in Quests) __t.Dispose();
            Quests.Clear();
            foreach (var __t in StoryFlags) __t.Dispose();
            StoryFlags.Clear();
            foreach (var __t in Items) __t.Dispose();
            Items.Clear();
            MessageObjectPool<G2C_LoadPlayerProgress>.Return(this);
        }

        /// <summary>复制协议字段到目标消息。</summary>
        /// <param name="other">接收副本字段的目标消息。</param>
        public void CopyTo(ref G2C_LoadPlayerProgress other)
        {
            if (other == null)
            {
                other = Create(false);
            }
            other.ErrorCode = ErrorCode;
            other.ErrorMessage = ErrorMessage;
            other.Revision = Revision;
            other.Quests = new List<PlayerQuestProgress>(Quests.Count);
            foreach (var __item in Quests) other.Quests.Add(__item.DeepCopy());
            other.StoryFlags = new List<PlayerStoryFlag>(StoryFlags.Count);
            foreach (var __item in StoryFlags) other.StoryFlags.Add(__item.DeepCopy());
            other.Items = new List<PlayerItemStack>(Items.Count);
            foreach (var __item in Items) other.Items.Add(__item.DeepCopy());

        }

        /// <summary>创建独立的消息副本。</summary>
        public G2C_LoadPlayerProgress DeepCopy()
        {
            var other = Create(false);
            CopyTo(ref other);
            return other;
        }
        /// <summary>取得本消息的协议编号。</summary>
        public uint OpCode() { return OuterOpcode.G2C_LoadPlayerProgress; } 
        /// <summary>协议字段 ErrorCode。</summary>
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        /// <summary>协议字段 ErrorMessage。</summary>
        [ProtoMember(2)]
        public string ErrorMessage { get; set; }
        /// <summary>协议字段 Revision。</summary>
        [ProtoMember(3)]
        public long Revision { get; set; }
        /// <summary>协议字段 Quests。</summary>
        [ProtoMember(4)]
        public List<PlayerQuestProgress> Quests { get; set; } = new List<PlayerQuestProgress>();
        /// <summary>协议字段 StoryFlags。</summary>
        [ProtoMember(5)]
        public List<PlayerStoryFlag> StoryFlags { get; set; } = new List<PlayerStoryFlag>();
        /// <summary>协议字段 Items。</summary>
        [ProtoMember(6)]
        public List<PlayerItemStack> Items { get; set; } = new List<PlayerItemStack>();
    }
    /// <summary>客户端提交一个幂等事件，账号身份始终由 Gate 认证会话决定。</summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_SubmitPlayerProgressEvent : AMessage, IRequest
    {
        /// <summary>从消息池创建协议对象。</summary>
        /// <param name="autoReturn">是否允许对象自动归还消息池。</param>
        public static C2G_SubmitPlayerProgressEvent Create(bool autoReturn = true)
        {
            var c2G_SubmitPlayerProgressEvent = MessageObjectPool<C2G_SubmitPlayerProgressEvent>.Rent();
            c2G_SubmitPlayerProgressEvent.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2G_SubmitPlayerProgressEvent.SetIsPool(false);
            }
            
            return c2G_SubmitPlayerProgressEvent;
        }
        
        /// <summary>归还当前协议对象。</summary>
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        /// <summary>清空字段并归还消息池。</summary>
        public void Dispose()
        {
            if (!IsPool()) return; 
            RequestId = default;
            EventType = default;
            QuestId = default;
            ObjectiveId = default;
            MapId = default;
            MapInstanceId = default;
            SourceId = default;
            Count = default;
            DialogueId = default;
            DialogueNodeId = default;
            MessageObjectPool<C2G_SubmitPlayerProgressEvent>.Return(this);
        }

        /// <summary>复制协议字段到目标消息。</summary>
        /// <param name="other">接收副本字段的目标消息。</param>
        public void CopyTo(ref C2G_SubmitPlayerProgressEvent other)
        {
            if (other == null)
            {
                other = Create(false);
            }
            other.RequestId = RequestId;
            other.EventType = EventType;
            other.QuestId = QuestId;
            other.ObjectiveId = ObjectiveId;
            other.MapId = MapId;
            other.MapInstanceId = MapInstanceId;
            other.SourceId = SourceId;
            other.Count = Count;
            other.DialogueId = DialogueId;
            other.DialogueNodeId = DialogueNodeId;

        }

        /// <summary>创建独立的消息副本。</summary>
        public C2G_SubmitPlayerProgressEvent DeepCopy()
        {
            var other = Create(false);
            CopyTo(ref other);
            return other;
        }
        /// <summary>取得本消息的协议编号。</summary>
        public uint OpCode() { return OuterOpcode.C2G_SubmitPlayerProgressEvent; } 
        /// <summary>请求对应的响应类型，仅供框架推断。</summary>
        [ProtoIgnore]
        public G2C_SubmitPlayerProgressEvent ResponseType { get; set; }
        /// <summary>协议字段 RequestId。</summary>
        [ProtoMember(1)]
        public string RequestId { get; set; }
        /// <summary>协议字段 EventType。</summary>
        [ProtoMember(2)]
        public int EventType { get; set; }
        /// <summary>协议字段 QuestId。</summary>
        [ProtoMember(3)]
        public int QuestId { get; set; }
        /// <summary>协议字段 ObjectiveId。</summary>
        [ProtoMember(4)]
        public int ObjectiveId { get; set; }
        /// <summary>协议字段 MapId。</summary>
        [ProtoMember(5)]
        public int MapId { get; set; }
        /// <summary>协议字段 MapInstanceId。</summary>
        [ProtoMember(6)]
        public int MapInstanceId { get; set; }
        /// <summary>协议字段 SourceId。</summary>
        [ProtoMember(7)]
        public int SourceId { get; set; }
        /// <summary>协议字段 Count。</summary>
        [ProtoMember(8)]
        public int Count { get; set; }
        /// <summary>协议字段 DialogueId。</summary>
        [ProtoMember(9)]
        public int DialogueId { get; set; }
        /// <summary>协议字段 DialogueNodeId。</summary>
        [ProtoMember(10)]
        public int DialogueNodeId { get; set; }
    }
    /// <summary>网络协议消息 G2C_SubmitPlayerProgressEvent。</summary>
    [Serializable]
    [ProtoContract]
    public partial class G2C_SubmitPlayerProgressEvent : AMessage, IResponse
    {
        /// <summary>从消息池创建协议对象。</summary>
        /// <param name="autoReturn">是否允许对象自动归还消息池。</param>
        public static G2C_SubmitPlayerProgressEvent Create(bool autoReturn = true)
        {
            var g2C_SubmitPlayerProgressEvent = MessageObjectPool<G2C_SubmitPlayerProgressEvent>.Rent();
            g2C_SubmitPlayerProgressEvent.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2C_SubmitPlayerProgressEvent.SetIsPool(false);
            }
            
            return g2C_SubmitPlayerProgressEvent;
        }
        
        /// <summary>归还当前协议对象。</summary>
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        /// <summary>清空字段并归还消息池。</summary>
        public void Dispose()
        {
            if (!IsPool()) return; 
            ErrorCode = 0;
            ErrorCode = default;
            ErrorMessage = default;
            Revision = default;
            foreach (var __t in Quests) __t.Dispose();
            Quests.Clear();
            foreach (var __t in StoryFlags) __t.Dispose();
            StoryFlags.Clear();
            foreach (var __t in Items) __t.Dispose();
            Items.Clear();
            MessageObjectPool<G2C_SubmitPlayerProgressEvent>.Return(this);
        }

        /// <summary>复制协议字段到目标消息。</summary>
        /// <param name="other">接收副本字段的目标消息。</param>
        public void CopyTo(ref G2C_SubmitPlayerProgressEvent other)
        {
            if (other == null)
            {
                other = Create(false);
            }
            other.ErrorCode = ErrorCode;
            other.ErrorMessage = ErrorMessage;
            other.Revision = Revision;
            other.Quests = new List<PlayerQuestProgress>(Quests.Count);
            foreach (var __item in Quests) other.Quests.Add(__item.DeepCopy());
            other.StoryFlags = new List<PlayerStoryFlag>(StoryFlags.Count);
            foreach (var __item in StoryFlags) other.StoryFlags.Add(__item.DeepCopy());
            other.Items = new List<PlayerItemStack>(Items.Count);
            foreach (var __item in Items) other.Items.Add(__item.DeepCopy());

        }

        /// <summary>创建独立的消息副本。</summary>
        public G2C_SubmitPlayerProgressEvent DeepCopy()
        {
            var other = Create(false);
            CopyTo(ref other);
            return other;
        }
        /// <summary>取得本消息的协议编号。</summary>
        public uint OpCode() { return OuterOpcode.G2C_SubmitPlayerProgressEvent; } 
        /// <summary>协议字段 ErrorCode。</summary>
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        /// <summary>协议字段 ErrorMessage。</summary>
        [ProtoMember(2)]
        public string ErrorMessage { get; set; }
        /// <summary>协议字段 Revision。</summary>
        [ProtoMember(3)]
        public long Revision { get; set; }
        /// <summary>协议字段 Quests。</summary>
        [ProtoMember(4)]
        public List<PlayerQuestProgress> Quests { get; set; } = new List<PlayerQuestProgress>();
        /// <summary>协议字段 StoryFlags。</summary>
        [ProtoMember(5)]
        public List<PlayerStoryFlag> StoryFlags { get; set; } = new List<PlayerStoryFlag>();
        /// <summary>协议字段 Items。</summary>
        [ProtoMember(6)]
        public List<PlayerItemStack> Items { get; set; } = new List<PlayerItemStack>();
    }
}
