using Entity.Gate.Compoent;
using Fantasy;
using Fantasy.Gate.Entity;
using Fantasy.Network;
using Hotfix.Auth;

namespace Hotfix.Gate.Component;

/// <summary>
/// 双人房间创建、加入、重连绑定和独立帧同步实体的生命周期逻辑。
/// </summary>
public static class FrameRoomManagerComponentSystem
{
    /// <summary>每局帧同步的固定成员数量。</summary>
    public const int RequiredPlayerCount = 2;

    /// <summary>
    /// 记录登录身份并在玩家处于进行中房间时重新绑定其会话。
    /// </summary>
    /// <param name="self">Gate 场景中的房间管理组件。</param>
    /// <param name="session">本次登录成功的会话。</param>
    /// <param name="playerId">认证后的玩家标识。</param>
    public static void BindLoginSession(this FrameRoomManagerComponent self, Session session, int playerId)
    {
        if (!self.PlayerRoomIds.TryGetValue(playerId, out int roomId) || !self.Rooms.TryGetValue(roomId, out FrameRoom? room))
        {
            return;
        }

        FrameRoomMember? member = room.Members.FirstOrDefault(item => item.PlayerId == playerId);
        if (member == null)
        {
            return;
        }

        member.Session = session;
        if (room.IsStarted && room.FrameManager != null && room.FrameManager.TryRebindSession(playerId, session, out _))
        {
            return;
        }
    }

    /// <summary>
    /// 创建一个等待第二名玩家加入的双人房间。
    /// </summary>
    /// <param name="self">Gate 场景中的房间管理组件。</param>
    /// <param name="session">创建者已登录的会话。</param>
    /// <param name="dungeonId">房间采用的副本配置标识。</param>
    /// <param name="dungeonType">房间采用的副本类型。</param>
    /// <param name="heroId">创建者选择的英雄配置标识。</param>
    /// <param name="roomId">成功时返回房间标识。</param>
    /// <param name="errorMessage">失败时返回可展示原因。</param>
    /// <returns>创建成功时返回 true。</returns>
    public static bool TryCreateRoom(this FrameRoomManagerComponent self, Session session, int dungeonId, int dungeonType,
        int heroId, out int roomId, out string errorMessage)
    {
        roomId = 0;
        errorMessage = string.Empty;
        if (!self.TryGetPlayerId(session, out int playerId))
        {
            errorMessage = "会话尚未登录";
            return false;
        }

        if (dungeonId <= 0 || heroId <= 0)
        {
            errorMessage = "副本或英雄配置无效";
            return false;
        }

        if (self.PlayerRoomIds.ContainsKey(playerId))
        {
            errorMessage = "玩家已经处于房间中";
            return false;
        }

        roomId = self.NextRoomId++;
        FrameRoom room = new()
        {
            RoomId = roomId,
            OwnerPlayerId = playerId,
            DungeonId = dungeonId,
            DungeonType = dungeonType,
        };
        room.Members.Add(new FrameRoomMember { PlayerId = playerId, HeroId = heroId, Session = session });
        self.Rooms.Add(roomId, room);
        self.PlayerRoomIds.Add(playerId, roomId);
        return true;
    }

    /// <summary>
    /// 将登录玩家加入等待中的双人房间，第二人加入后立即启动对局。
    /// </summary>
    /// <param name="self">Gate 场景中的房间管理组件。</param>
    /// <param name="session">加入者已登录的会话。</param>
    /// <param name="roomId">目标房间标识。</param>
    /// <param name="heroId">加入者选择的英雄配置标识。</param>
    /// <param name="errorMessage">失败时返回可展示原因。</param>
    /// <returns>加入并成功启动对局时返回 true。</returns>
    public static bool TryJoinRoom(this FrameRoomManagerComponent self, Session session, int roomId, int heroId,
        out string errorMessage)
    {
        errorMessage = string.Empty;
        if (!self.TryGetPlayerId(session, out int playerId))
        {
            errorMessage = "会话尚未登录";
            return false;
        }

        if (heroId <= 0 || !self.Rooms.TryGetValue(roomId, out FrameRoom? room))
        {
            errorMessage = "房间或英雄配置无效";
            return false;
        }

        if (self.PlayerRoomIds.ContainsKey(playerId))
        {
            errorMessage = "玩家已经处于房间中";
            return false;
        }

        if (room.IsStarted || room.Members.Count != RequiredPlayerCount - 1)
        {
            errorMessage = "房间不可加入";
            return false;
        }

        room.Members.Add(new FrameRoomMember { PlayerId = playerId, HeroId = heroId, Session = session });
        self.PlayerRoomIds.Add(playerId, roomId);
        self.StartRoom(room);
        return true;
    }

    /// <summary>
    /// 从会话定位其所属对局的帧同步实体和单位。
    /// </summary>
    /// <param name="self">Gate 场景中的房间管理组件。</param>
    /// <param name="session">发送帧同步消息的会话。</param>
    /// <param name="frameManager">成功时返回对应对局的帧同步实体。</param>
    /// <param name="unit">成功时返回经过会话验证的单位。</param>
    /// <returns>会话属于一个进行中对局时返回 true。</returns>
    public static bool TryGetPlayingUnit(this FrameRoomManagerComponent self, Session session,
        out FSManagerComponent frameManager, out Unit unit)
    {
        if (self.TryGetRoomBySession(session, out FrameRoom? room) && room.IsStarted && room.FrameManager != null &&
            room.FrameManager.TryGetUnitBySession(session, out unit))
        {
            frameManager = room.FrameManager;
            return true;
        }

        frameManager = null!;
        unit = null!;
        return false;
    }

    /// <summary>
    /// 校验会话归属的对局标识并返回其帧同步实体。
    /// </summary>
    /// <param name="self">Gate 场景中的房间管理组件。</param>
    /// <param name="session">请求补帧的会话。</param>
    /// <param name="matchId">客户端提交的对局标识。</param>
    /// <param name="frameManager">成功时返回对应对局的帧同步实体。</param>
    /// <param name="unit">成功时返回请求者单位。</param>
    /// <returns>会话与对局标识匹配时返回 true。</returns>
    public static bool TryGetMatchBySession(this FrameRoomManagerComponent self, Session session, int matchId,
        out FSManagerComponent frameManager, out Unit unit)
    {
        if (matchId > 0 && self.TryGetPlayingUnit(session, out frameManager, out unit) &&
            frameManager.MatchDescriptor?.MatchId == matchId)
        {
            return true;
        }

        frameManager = null!;
        unit = null!;
        return false;
    }

    /// <summary>
    /// 销毁结束的房间和其独立帧同步状态。
    /// </summary>
    /// <param name="self">Gate 场景中的房间管理组件。</param>
    /// <param name="roomId">需要销毁的房间标识。</param>
    public static void DestroyRoom(this FrameRoomManagerComponent self, int roomId)
    {
        if (!self.Rooms.Remove(roomId, out FrameRoom? room))
        {
            return;
        }

        foreach (FrameRoomMember member in room.Members)
        {
            self.PlayerRoomIds.Remove(member.PlayerId);
            member.Unit?.Dispose();
            member.Unit = null;
            member.Session = null;
        }

        if (room.FrameManager != null)
        {
            room.FrameManager.ClearMatchState();
            room.FrameManager.Dispose();
            room.FrameManager = null;
        }
    }

    /// <summary>
    /// 将已完成成员配置的房间转换为一个独立运行的确定性对局。
    /// </summary>
    /// <param name="self">Gate 场景中的房间管理组件。</param>
    /// <param name="room">即将启动的完整双人房间。</param>
    private static void StartRoom(this FrameRoomManagerComponent self, FrameRoom room)
    {
        if (room.Members.Count != RequiredPlayerCount || room.IsStarted)
        {
            return;
        }

        List<FrameMatchPlayerDescriptor> players = room.Members
            .OrderBy(member => member.PlayerId)
            .Select((member, index) => new FrameMatchPlayerDescriptor
            {
                PlayerId = member.PlayerId,
                Position = index,
                HeroIds = new[] { member.HeroId },
            })
            .ToList();
        FrameMatchDescriptor descriptor = new()
        {
            MatchId = room.RoomId,
            RandomSeed = Random.Shared.Next(1, int.MaxValue),
            DungeonId = room.DungeonId,
            DungeonType = room.DungeonType,
            Players = players,
        };

        FSManagerComponent frameManager = Fantasy.Entitas.Entity.Create<FSManagerComponent>(self.Scene);
        frameManager.ConfigureMatch(descriptor);
        room.FrameManager = frameManager;
        foreach (FrameRoomMember member in room.Members)
        {
            Unit unit = Fantasy.Entitas.Entity.Create<Unit>(self.Scene);
            unit.UnitId = member.PlayerId;
            unit.Session = member.Session;
            if (!frameManager.Join(unit))
            {
                unit.Dispose();
                self.DestroyRoom(room.RoomId);
                return;
            }

            member.Unit = unit;
        }

        room.IsStarted = true;
        foreach (FrameRoomMember member in room.Members)
        {
            if (member.Unit != null)
            {
                frameManager.SendMatchBootstrap(member.Unit);
            }
        }
    }

    /// <summary>
    /// 从会话找到已认证玩家标识。
    /// </summary>
    /// <param name="self">Gate 场景中的房间管理组件。</param>
    /// <param name="session">需要查询的会话。</param>
    /// <param name="playerId">成功时返回的玩家标识。</param>
    /// <returns>会话已经完成登录时返回 true。</returns>
    private static bool TryGetPlayerId(this FrameRoomManagerComponent self, Session session, out int playerId)
    {
        return AuthSceneHelper.TryGetPlayerId(session, out playerId);
    }

    /// <summary>
    /// 从会话找到该玩家所在的房间。
    /// </summary>
    /// <param name="self">Gate 场景中的房间管理组件。</param>
    /// <param name="session">需要查询的会话。</param>
    /// <param name="room">成功时返回的房间。</param>
    /// <returns>会话属于现存房间时返回 true。</returns>
    private static bool TryGetRoomBySession(this FrameRoomManagerComponent self, Session session, out FrameRoom room)
    {
        if (self.TryGetPlayerId(session, out int playerId) && self.PlayerRoomIds.TryGetValue(playerId, out int roomId) &&
            self.Rooms.TryGetValue(roomId, out room!))
        {
            return true;
        }

        room = null!;
        return false;
    }
}
