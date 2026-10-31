using System.Runtime.CompilerServices;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using System.Collections.Generic;
#pragma warning disable CS8618
namespace Fantasy
{
   /// <summary>网络协议辅助定义 NetworkProtocolHelper。</summary>
   public static class NetworkProtocolHelper
   {
		/// <summary>发送 C2G_TestMessage 协议请求并等待响应。</summary>
		/// <param name="session">已经建立的网络会话。</param>
		/// <param name="C2G_TestMessage_message">协议参数 C2G_TestMessage_message。</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_TestMessage(this Session session, C2G_TestMessage C2G_TestMessage_message)
		{
			session.Send(C2G_TestMessage_message);
		}
		/// <summary>发送 C2G_TestMessage 协议请求并等待响应。</summary>
		/// <param name="session">已经建立的网络会话。</param>
		/// <param name="tag">协议参数 tag。</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_TestMessage(this Session session, string tag)
		{
			using var C2G_TestMessage_message = Fantasy.C2G_TestMessage.Create();
			C2G_TestMessage_message.Tag = tag;
			session.Send(C2G_TestMessage_message);
		}
		/// <summary>发送 C2G_TestRequest 协议请求并等待响应。</summary>
		/// <param name="session">已经建立的网络会话。</param>
		/// <param name="C2G_TestRequest_request">协议参数 C2G_TestRequest_request。</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_TestResponse> C2G_TestRequest(this Session session, C2G_TestRequest C2G_TestRequest_request)
		{
			return (G2C_TestResponse)await session.Call(C2G_TestRequest_request);
		}
		/// <summary>发送 C2G_TestRequest 协议请求并等待响应。</summary>
		/// <param name="session">已经建立的网络会话。</param>
		/// <param name="tag">协议参数 tag。</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_TestResponse> C2G_TestRequest(this Session session, string tag)
		{
			using var C2G_TestRequest_request = Fantasy.C2G_TestRequest.Create();
			C2G_TestRequest_request.Tag = tag;
			return (G2C_TestResponse)await session.Call(C2G_TestRequest_request);
		}
		/// <summary>发送 C2G_LoginRequest 协议请求并等待响应。</summary>
		/// <param name="session">已经建立的网络会话。</param>
		/// <param name="C2G_LoginRequest_request">协议参数 C2G_LoginRequest_request。</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_LoginResponse> C2G_LoginRequest(this Session session, C2G_LoginRequest C2G_LoginRequest_request)
		{
			return (G2C_LoginResponse)await session.Call(C2G_LoginRequest_request);
		}
		/// <summary>发送 C2G_LoginRequest 协议请求并等待响应。</summary>
		/// <param name="session">已经建立的网络会话。</param>
		/// <param name="id">协议参数 id。</param>
		/// <param name="ticket">协议参数 ticket。</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_LoginResponse> C2G_LoginRequest(this Session session, int id, string ticket)
		{
			using var C2G_LoginRequest_request = Fantasy.C2G_LoginRequest.Create();
			C2G_LoginRequest_request.Id = id;
			C2G_LoginRequest_request.Ticket = ticket;
			return (G2C_LoginResponse)await session.Call(C2G_LoginRequest_request);
		}
		/// <summary>发送 G2C_LoginRevoked 协议请求并等待响应。</summary>
		/// <param name="session">已经建立的网络会话。</param>
		/// <param name="G2C_LoginRevoked_message">协议参数 G2C_LoginRevoked_message。</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2C_LoginRevoked(this Session session, G2C_LoginRevoked G2C_LoginRevoked_message)
		{
			session.Send(G2C_LoginRevoked_message);
		}
		/// <summary>发送 G2C_LoginRevoked 协议请求并等待响应。</summary>
		/// <param name="session">已经建立的网络会话。</param>
		/// <param name="reason">协议参数 reason。</param>
		/// <param name="message">协议参数 message。</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2C_LoginRevoked(this Session session, uint reason, string message)
		{
			using var G2C_LoginRevoked_message = Fantasy.G2C_LoginRevoked.Create();
			G2C_LoginRevoked_message.Reason = reason;
			G2C_LoginRevoked_message.Message = message;
			session.Send(G2C_LoginRevoked_message);
		}
		/// <summary>发送 G2C_FrameCommand 协议请求并等待响应。</summary>
		/// <param name="session">已经建立的网络会话。</param>
		/// <param name="G2C_FrameCommand_message">协议参数 G2C_FrameCommand_message。</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2C_FrameCommand(this Session session, G2C_FrameCommand G2C_FrameCommand_message)
		{
			session.Send(G2C_FrameCommand_message);
		}
		/// <summary>发送 G2C_FrameCommand 协议请求并等待响应。</summary>
		/// <param name="session">已经建立的网络会话。</param>
		/// <param name="frameId">协议参数 frameId。</param>
		/// <param name="commands">协议参数 commands。</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2C_FrameCommand(this Session session, int frameId, List<OneFrameCommand> commands)
		{
			using var G2C_FrameCommand_message = Fantasy.G2C_FrameCommand.Create();
			G2C_FrameCommand_message.FrameId = frameId;
			G2C_FrameCommand_message.Commands = commands;
			session.Send(G2C_FrameCommand_message);
		}
		/// <summary>发送 C2G_NextFrameCommand 协议请求并等待响应。</summary>
		/// <param name="session">已经建立的网络会话。</param>
		/// <param name="C2G_NextFrameCommand_message">协议参数 C2G_NextFrameCommand_message。</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_NextFrameCommand(this Session session, C2G_NextFrameCommand C2G_NextFrameCommand_message)
		{
			session.Send(C2G_NextFrameCommand_message);
		}
		/// <summary>发送 C2G_NextFrameCommand 协议请求并等待响应。</summary>
		/// <param name="session">已经建立的网络会话。</param>
		/// <param name="ackFrameId">协议参数 ackFrameId。</param>
		/// <param name="commands">协议参数 commands。</param>
		/// <param name="inputSequence">协议参数 inputSequence。</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_NextFrameCommand(this Session session, int ackFrameId, List<FrameCommand> commands, int inputSequence)
		{
			using var C2G_NextFrameCommand_message = Fantasy.C2G_NextFrameCommand.Create();
			C2G_NextFrameCommand_message.AckFrameId = ackFrameId;
			C2G_NextFrameCommand_message.Commands = commands;
			C2G_NextFrameCommand_message.InputSequence = inputSequence;
			session.Send(C2G_NextFrameCommand_message);
		}
		/// <summary>发送 C2G_FrameAck 协议请求并等待响应。</summary>
		/// <param name="session">已经建立的网络会话。</param>
		/// <param name="C2G_FrameAck_message">协议参数 C2G_FrameAck_message。</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_FrameAck(this Session session, C2G_FrameAck C2G_FrameAck_message)
		{
			session.Send(C2G_FrameAck_message);
		}
		/// <summary>发送 C2G_FrameAck 协议请求并等待响应。</summary>
		/// <param name="session">已经建立的网络会话。</param>
		/// <param name="ackFrameId">协议参数 ackFrameId。</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_FrameAck(this Session session, int ackFrameId)
		{
			using var C2G_FrameAck_message = Fantasy.C2G_FrameAck.Create();
			C2G_FrameAck_message.AckFrameId = ackFrameId;
			session.Send(C2G_FrameAck_message);
		}
		/// <summary>发送 C2G_FrameFetchRequest 协议请求并等待响应。</summary>
		/// <param name="session">已经建立的网络会话。</param>
		/// <param name="C2G_FrameFetchRequest_message">协议参数 C2G_FrameFetchRequest_message。</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_FrameFetchRequest(this Session session, C2G_FrameFetchRequest C2G_FrameFetchRequest_message)
		{
			session.Send(C2G_FrameFetchRequest_message);
		}
		/// <summary>发送 C2G_FrameFetchRequest 协议请求并等待响应。</summary>
		/// <param name="session">已经建立的网络会话。</param>
		/// <param name="matchId">协议参数 matchId。</param>
		/// <param name="startFrameId">协议参数 startFrameId。</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_FrameFetchRequest(this Session session, int matchId, int startFrameId)
		{
			using var C2G_FrameFetchRequest_message = Fantasy.C2G_FrameFetchRequest.Create();
			C2G_FrameFetchRequest_message.MatchId = matchId;
			C2G_FrameFetchRequest_message.StartFrameId = startFrameId;
			session.Send(C2G_FrameFetchRequest_message);
		}
		/// <summary>发送 C2G_CreateFrameRoom 协议请求并等待响应。</summary>
		/// <param name="session">已经建立的网络会话。</param>
		/// <param name="C2G_CreateFrameRoom_request">协议参数 C2G_CreateFrameRoom_request。</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_CreateFrameRoom> C2G_CreateFrameRoom(this Session session, C2G_CreateFrameRoom C2G_CreateFrameRoom_request)
		{
			return (G2C_CreateFrameRoom)await session.Call(C2G_CreateFrameRoom_request);
		}
		/// <summary>发送 C2G_CreateFrameRoom 协议请求并等待响应。</summary>
		/// <param name="session">已经建立的网络会话。</param>
		/// <param name="dungeonId">协议参数 dungeonId。</param>
		/// <param name="dungeonType">协议参数 dungeonType。</param>
		/// <param name="heroId">协议参数 heroId。</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_CreateFrameRoom> C2G_CreateFrameRoom(this Session session, int dungeonId, int dungeonType, int heroId)
		{
			using var C2G_CreateFrameRoom_request = Fantasy.C2G_CreateFrameRoom.Create();
			C2G_CreateFrameRoom_request.DungeonId = dungeonId;
			C2G_CreateFrameRoom_request.DungeonType = dungeonType;
			C2G_CreateFrameRoom_request.HeroId = heroId;
			return (G2C_CreateFrameRoom)await session.Call(C2G_CreateFrameRoom_request);
		}
		/// <summary>发送 C2G_JoinFrameRoom 协议请求并等待响应。</summary>
		/// <param name="session">已经建立的网络会话。</param>
		/// <param name="C2G_JoinFrameRoom_request">协议参数 C2G_JoinFrameRoom_request。</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_JoinFrameRoom> C2G_JoinFrameRoom(this Session session, C2G_JoinFrameRoom C2G_JoinFrameRoom_request)
		{
			return (G2C_JoinFrameRoom)await session.Call(C2G_JoinFrameRoom_request);
		}
		/// <summary>发送 C2G_JoinFrameRoom 协议请求并等待响应。</summary>
		/// <param name="session">已经建立的网络会话。</param>
		/// <param name="roomId">协议参数 roomId。</param>
		/// <param name="heroId">协议参数 heroId。</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_JoinFrameRoom> C2G_JoinFrameRoom(this Session session, int roomId, int heroId)
		{
			using var C2G_JoinFrameRoom_request = Fantasy.C2G_JoinFrameRoom.Create();
			C2G_JoinFrameRoom_request.RoomId = roomId;
			C2G_JoinFrameRoom_request.HeroId = heroId;
			return (G2C_JoinFrameRoom)await session.Call(C2G_JoinFrameRoom_request);
		}
		/// <summary>发送 G2C_FrameMatchBootstrap 协议请求并等待响应。</summary>
		/// <param name="session">已经建立的网络会话。</param>
		/// <param name="G2C_FrameMatchBootstrap_message">协议参数 G2C_FrameMatchBootstrap_message。</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2C_FrameMatchBootstrap(this Session session, G2C_FrameMatchBootstrap G2C_FrameMatchBootstrap_message)
		{
			session.Send(G2C_FrameMatchBootstrap_message);
		}
		/// <summary>发送 G2C_FrameMatchBootstrap 协议请求并等待响应。</summary>
		/// <param name="session">已经建立的网络会话。</param>
		/// <param name="matchId">协议参数 matchId。</param>
		/// <param name="randomSeed">协议参数 randomSeed。</param>
		/// <param name="dungeonId">协议参数 dungeonId。</param>
		/// <param name="dungeonType">协议参数 dungeonType。</param>
		/// <param name="currentFrameId">协议参数 currentFrameId。</param>
		/// <param name="players">协议参数 players。</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2C_FrameMatchBootstrap(this Session session, int matchId, int randomSeed, int dungeonId, int dungeonType, int currentFrameId, List<FrameMatchPlayer> players)
		{
			using var G2C_FrameMatchBootstrap_message = Fantasy.G2C_FrameMatchBootstrap.Create();
			G2C_FrameMatchBootstrap_message.MatchId = matchId;
			G2C_FrameMatchBootstrap_message.RandomSeed = randomSeed;
			G2C_FrameMatchBootstrap_message.DungeonId = dungeonId;
			G2C_FrameMatchBootstrap_message.DungeonType = dungeonType;
			G2C_FrameMatchBootstrap_message.CurrentFrameId = currentFrameId;
			G2C_FrameMatchBootstrap_message.Players = players;
			session.Send(G2C_FrameMatchBootstrap_message);
		}
		/// <summary>发送 C2G_LoadPlayerProgress 协议请求并等待响应。</summary>
		/// <param name="session">已经建立的网络会话。</param>
		/// <param name="C2G_LoadPlayerProgress_request">协议参数 C2G_LoadPlayerProgress_request。</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_LoadPlayerProgress> C2G_LoadPlayerProgress(this Session session, C2G_LoadPlayerProgress C2G_LoadPlayerProgress_request)
		{
			return (G2C_LoadPlayerProgress)await session.Call(C2G_LoadPlayerProgress_request);
		}
		/// <summary>发送 C2G_LoadPlayerProgress 协议请求并等待响应。</summary>
		/// <param name="session">已经建立的网络会话。</param>
		/// <param name="mapId">协议参数 mapId。</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_LoadPlayerProgress> C2G_LoadPlayerProgress(this Session session, int mapId)
		{
			using var C2G_LoadPlayerProgress_request = Fantasy.C2G_LoadPlayerProgress.Create();
			C2G_LoadPlayerProgress_request.MapId = mapId;
			return (G2C_LoadPlayerProgress)await session.Call(C2G_LoadPlayerProgress_request);
		}
		/// <summary>发送 C2G_SubmitPlayerProgressEvent 协议请求并等待响应。</summary>
		/// <param name="session">已经建立的网络会话。</param>
		/// <param name="C2G_SubmitPlayerProgressEvent_request">协议参数 C2G_SubmitPlayerProgressEvent_request。</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_SubmitPlayerProgressEvent> C2G_SubmitPlayerProgressEvent(this Session session, C2G_SubmitPlayerProgressEvent C2G_SubmitPlayerProgressEvent_request)
		{
			return (G2C_SubmitPlayerProgressEvent)await session.Call(C2G_SubmitPlayerProgressEvent_request);
		}
		/// <summary>发送 C2G_SubmitPlayerProgressEvent 协议请求并等待响应。</summary>
		/// <param name="session">已经建立的网络会话。</param>
		/// <param name="requestId">协议参数 requestId。</param>
		/// <param name="eventType">协议参数 eventType。</param>
		/// <param name="questId">协议参数 questId。</param>
		/// <param name="objectiveId">协议参数 objectiveId。</param>
		/// <param name="mapId">协议参数 mapId。</param>
		/// <param name="mapInstanceId">协议参数 mapInstanceId。</param>
		/// <param name="sourceId">协议参数 sourceId。</param>
		/// <param name="count">协议参数 count。</param>
		/// <param name="dialogueId">协议参数 dialogueId。</param>
		/// <param name="dialogueNodeId">协议参数 dialogueNodeId。</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_SubmitPlayerProgressEvent> C2G_SubmitPlayerProgressEvent(this Session session, string requestId, int eventType, int questId, int objectiveId, int mapId, int mapInstanceId, int sourceId, int count, int dialogueId, int dialogueNodeId)
		{
			using var C2G_SubmitPlayerProgressEvent_request = Fantasy.C2G_SubmitPlayerProgressEvent.Create();
			C2G_SubmitPlayerProgressEvent_request.RequestId = requestId;
			C2G_SubmitPlayerProgressEvent_request.EventType = eventType;
			C2G_SubmitPlayerProgressEvent_request.QuestId = questId;
			C2G_SubmitPlayerProgressEvent_request.ObjectiveId = objectiveId;
			C2G_SubmitPlayerProgressEvent_request.MapId = mapId;
			C2G_SubmitPlayerProgressEvent_request.MapInstanceId = mapInstanceId;
			C2G_SubmitPlayerProgressEvent_request.SourceId = sourceId;
			C2G_SubmitPlayerProgressEvent_request.Count = count;
			C2G_SubmitPlayerProgressEvent_request.DialogueId = dialogueId;
			C2G_SubmitPlayerProgressEvent_request.DialogueNodeId = dialogueNodeId;
			return (G2C_SubmitPlayerProgressEvent)await session.Call(C2G_SubmitPlayerProgressEvent_request);
		}

   }
}
