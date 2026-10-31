using System.Security.Cryptography;
using System.Text;
using Entity.Auth;
using Fantasy;
using Fantasy.Network;
using Hotfix.Gate.Component;
using MongoDB.Bson;
using MongoDB.Driver;
using cfg.Dialogue;
using cfg.Map;
using cfg.Quest;
using ItemTables = cfg.item;

namespace Hotfix.Progress;

/// <summary>玩家进度服务对外返回的稳定错误码。</summary>
internal static class ProgressError
{
    /// <summary>请求已成功处理。</summary>
    public const uint Success = 0;
    /// <summary>当前网络会话未通过认证。</summary>
    public const uint NotAuthenticated = 1;
    /// <summary>请求编号或字段组合无效。</summary>
    public const uint InvalidRequest = 2;
    /// <summary>任务尚未达到开放条件。</summary>
    public const uint QuestLocked = 3;
    /// <summary>任务当前未处于进行中状态。</summary>
    public const uint QuestNotActive = 4;
    /// <summary>事件来源与任务配置不一致。</summary>
    public const uint InvalidEventSource = 5;
    /// <summary>目标阶段尚未开放。</summary>
    public const uint InvalidObjectiveOrder = 6;
    /// <summary>服务端任务配置缺失或不合法。</summary>
    public const uint InvalidContentConfig = 7;
    /// <summary>账号文档发生并发更新冲突。</summary>
    public const uint RevisionConflict = 8;
    /// <summary>任务进度存储暂时不可用。</summary>
    public const uint StorageUnavailable = 9;
}

/// <summary>保存在账号进度文档中的任务状态值。</summary>
internal static class QuestStatusCode
{
    /// <summary>任务尚未开放。</summary>
    public const int Locked = 0;
    /// <summary>任务可由玩家接取。</summary>
    public const int Available = 1;
    /// <summary>任务正在进行。</summary>
    public const int Active = 2;
    /// <summary>任务已经完成。</summary>
    public const int Completed = 3;
}

/// <summary>服务端接受的进度事件类型。</summary>
internal enum ProgressEventType
{
    /// <summary>在配置 NPC 处接取任务。</summary>
    AcceptQuest = 1,
    /// <summary>提交任务目标进度。</summary>
    ObjectiveProgress = 2,
    /// <summary>确认任务简报。</summary>
    AcknowledgeBriefing = 3,
    /// <summary>提交配置对话中的剧情标记节点。</summary>
    DialogueSetFlag = 4
}

/// <summary>从客户端 RPC 复制并校验的进度事件参数。</summary>
internal sealed class ProgressRequest
{
    /// <summary>用于超时重试去重的 GUID 请求编号。</summary>
    public string RequestId = string.Empty;
    /// <summary>本次进度操作类型。</summary>
    public int EventType;
    /// <summary>关联的任务编号。</summary>
    public int QuestId;
    /// <summary>关联的目标编号。</summary>
    public int ObjectiveId;
    /// <summary>事件发生的地图编号。</summary>
    public int MapId;
    /// <summary>地图内固定摆放实例编号。</summary>
    public int MapInstanceId;
    /// <summary>NPC、地图物件或敌人配置编号。</summary>
    public int SourceId;
    /// <summary>本次报告的操作数量。</summary>
    public int Count;
    /// <summary>关联的对话配置编号。</summary>
    public int DialogueId;
    /// <summary>关联的对话节点编号。</summary>
    public int DialogueNodeId;
}

/// <summary>进度请求不满足配置或业务规则时返回的领域异常。</summary>
internal sealed class ProgressOperationException : Exception
{
    /// <summary>对应的稳定进度服务错误码。</summary>
    public uint ErrorCode { get; }

    /// <summary>创建带稳定错误编号的进度异常。</summary>
    /// <param name="code">供 RPC 返回客户端的错误码。</param>
    /// <param name="message">仅用于服务端日志和诊断的说明。</param>
    public ProgressOperationException(uint code, string message) : base(message)
    {
        ErrorCode = code;
    }
}

/// <summary>账号级任务、剧情标记、物品堆叠与奖励收据的单文档 CAS 存储。</summary>
internal sealed class PlayerProgressStore
{
    /// <summary>当前进度文档的结构版本。</summary>
    private const int SchemaVersion = 1;
    /// <summary>每个文档保留的近期幂等请求上限。</summary>
    private const int MaximumRecentRequests = 256;
    /// <summary>账号进度版本冲突后的最大重读次数。</summary>
    private const int MaximumCasAttempts = 5;
    /// <summary>账号级任务进度 MongoDB 集合。</summary>
    private readonly IMongoCollection<BsonDocument> _collection;
    /// <summary>处理进度验证所需的已加载 Luban 配置。</summary>
    private readonly cfg.Tables _tables;

    /// <summary>绑定 MongoDB 进度集合和当前服务器配置。</summary>
    /// <param name="database">当前认证 Scene 使用的 MongoDB 数据库。</param>
    /// <param name="tables">服务端已加载的 Luban 配置集合。</param>
    public PlayerProgressStore(IMongoDatabase database, cfg.Tables tables)
    {
        _collection = database.GetCollection<BsonDocument>("player_progress");
        _tables = tables ?? throw new ArgumentNullException(nameof(tables));
    }

    /// <summary>加载玩家账号进度，并在进入地图时更新自动开放的任务。</summary>
    /// <param name="playerId">从已认证会话取得的玩家编号。</param>
    /// <param name="mapId">玩家进入的地图配置编号。</param>
    /// <returns>最新的完整账号进度文档。</returns>
    public async Task<BsonDocument> LoadForMapAsync(int playerId, int mapId)
    {
        if (playerId <= 0 || mapId < 0)
            throw new ProgressOperationException(ProgressError.InvalidRequest, "Invalid player or map id.");

        for (int attempt = 0; attempt < MaximumCasAttempts; attempt++)
        {
            BsonDocument? original = await ReadAsync(playerId);
            BsonDocument working = original?.DeepClone().AsBsonDocument ?? NewDocument(playerId);
            ValidateDocument(working);
            bool changed = RefreshAvailability(working, mapId);
            if (original == null)
            {
                working["revision"] = 1L;
                working["updatedAtUtc"] = DateTime.UtcNow;
                try
                {
                    await _collection.InsertOneAsync(working);
                    return working;
                }
                catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
                {
                    continue;
                }
            }
            if (!changed) return original;
            return await CompareAndSwapAsync(original, working);
        }

        throw new ProgressOperationException(ProgressError.RevisionConflict, "Progress changed during map entry.");
    }

    /// <summary>校验并原子保存一个任务、剧情标记或简报事件。</summary>
    /// <param name="playerId">从已认证会话取得的玩家编号。</param>
    /// <param name="request">已从 RPC 复制的进度事件参数。</param>
    /// <returns>变更后的完整账号进度文档。</returns>
    public async Task<BsonDocument> SubmitAsync(int playerId, ProgressRequest request)
    {
        ValidateRequestShape(request);
        string fingerprint = Fingerprint(request);
        for (int attempt = 0; attempt < MaximumCasAttempts; attempt++)
        {
            BsonDocument? original = await ReadAsync(playerId);
            BsonDocument working = original?.DeepClone().AsBsonDocument ?? NewDocument(playerId);
            ValidateDocument(working);

            BsonDocument? duplicate = FindRequest(working, request.RequestId);
            if (duplicate != null)
            {
                if (duplicate["fingerprint"].AsString != fingerprint)
                    throw new ProgressOperationException(ProgressError.InvalidRequest, "Request id was reused with different content.");
                return working;
            }

            ApplyEvent(working, request);
            RefreshAvailability(working, request.MapId);
            RememberRequest(working, request.RequestId, fingerprint);
            long oldRevision = original == null ? 0L : original["revision"].ToInt64();
            working["revision"] = oldRevision + 1;
            working["updatedAtUtc"] = DateTime.UtcNow;

            if (original == null)
            {
                try
                {
                    await _collection.InsertOneAsync(working);
                    return working;
                }
                catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
                {
                    continue;
                }
            }
            try { return await CompareAndSwapAsync(original, working); }
            catch (CasConflictException) { continue; }
        }
        throw new ProgressOperationException(ProgressError.RevisionConflict, "Progress changed during update.");
    }

    /// <summary>把账号文档复制到进度加载 RPC 响应对象。</summary>
    /// <param name="document">已校验的账号进度文档。</param>
    /// <param name="response">当前 RPC 的加载响应。</param>
    public static void FillSnapshot(BsonDocument document, G2C_LoadPlayerProgress response)
    {
        FillSnapshotFields(document, response.Quests, response.StoryFlags, response.Items);
        response.Revision = document["revision"].ToInt64();
    }

    /// <summary>把账号文档复制到进度变更 RPC 响应对象。</summary>
    /// <param name="document">已校验的账号进度文档。</param>
    /// <param name="response">当前 RPC 的变更响应。</param>
    public static void FillSnapshot(BsonDocument document, G2C_SubmitPlayerProgressEvent response)
    {
        FillSnapshotFields(document, response.Quests, response.StoryFlags, response.Items);
        response.Revision = document["revision"].ToInt64();
    }

    /// <summary>复制任务、剧情标记和物品堆叠到响应集合。</summary>
    /// <param name="document">账号进度 MongoDB 文档。</param>
    /// <param name="quests">任务进度响应列表。</param>
    /// <param name="flags">剧情标记响应列表。</param>
    /// <param name="items">物品堆叠响应列表。</param>
    private static void FillSnapshotFields(BsonDocument document, List<PlayerQuestProgress> quests,
        List<PlayerStoryFlag> flags, List<PlayerItemStack> items)
    {
        foreach (BsonDocument value in document["quests"].AsBsonArray.Select(item => item.AsBsonDocument))
        {
            var quest = PlayerQuestProgress.Create();
            quest.QuestId = value["questId"].ToInt32();
            quest.Status = value["status"].ToInt32();
            quest.BriefingAcknowledged = value["briefingAcknowledged"].ToBoolean();
            foreach (BsonDocument objective in value["objectives"].AsBsonArray.Select(item => item.AsBsonDocument))
            {
                var progress = PlayerObjectiveProgress.Create();
                progress.ObjectiveId = objective["objectiveId"].ToInt32();
                progress.Count = objective["count"].ToInt32();
                quest.Objectives.Add(progress);
            }
            quests.Add(quest);
        }
        foreach (BsonDocument value in document["storyFlags"].AsBsonArray.Select(item => item.AsBsonDocument))
        {
            var flag = PlayerStoryFlag.Create();
            flag.Key = value["key"].AsString;
            flag.Value = value["value"].ToBoolean();
            flags.Add(flag);
        }
        foreach (BsonDocument value in document["items"].AsBsonArray.Select(item => item.AsBsonDocument))
        {
            var stack = PlayerItemStack.Create();
            stack.ItemId = value["itemId"].ToInt32();
            stack.Count = value["count"].ToInt32();
            items.Add(stack);
        }
    }

    /// <summary>按账号主键读取现有进度文档。</summary>
    /// <param name="playerId">已认证玩家编号。</param>
    /// <returns>找到的进度文档；首次访问时返回空值。</returns>
    private async Task<BsonDocument?> ReadAsync(int playerId) =>
        await _collection.Find(Builders<BsonDocument>.Filter.Eq("_id", playerId)).FirstOrDefaultAsync();

    /// <summary>仅当文档版本未变化时原子替换进度。</summary>
    /// <param name="original">读取时的原始文档及版本。</param>
    /// <param name="working">已验证并完成修改的文档副本。</param>
    /// <returns>成功保存的文档。</returns>
    private async Task<BsonDocument> CompareAndSwapAsync(BsonDocument original, BsonDocument working)
    {
        int playerId = original["_id"].ToInt32();
        long revision = original["revision"].ToInt64();
        var filter = Builders<BsonDocument>.Filter.Eq("_id", playerId) & Builders<BsonDocument>.Filter.Eq("revision", revision);
        ReplaceOneResult result = await _collection.ReplaceOneAsync(filter, working, new ReplaceOptions { IsUpsert = false });
        if (result.ModifiedCount != 1) throw new CasConflictException();
        return working;
    }

    /// <summary>按事件类型调用对应验证和状态更新逻辑。</summary>
    /// <param name="document">本次事务中的账号文档副本。</param>
    /// <param name="request">已经通过基础字段校验的事件。</param>
    private void ApplyEvent(BsonDocument document, ProgressRequest request)
    {
        switch ((ProgressEventType)request.EventType)
        {
            case ProgressEventType.AcceptQuest: AcceptQuest(document, request); break;
            case ProgressEventType.ObjectiveProgress: ApplyObjective(document, request); break;
            case ProgressEventType.AcknowledgeBriefing: AcknowledgeBriefing(document, request); break;
            case ProgressEventType.DialogueSetFlag: SetDialogueFlag(document, request); break;
            default: throw new ProgressOperationException(ProgressError.InvalidRequest, "Unknown progress event.");
        }
    }

    /// <summary>验证委托 NPC 与任务开放状态后接取任务。</summary>
    /// <param name="document">本次事务中的账号文档副本。</param>
    /// <param name="request">接取任务事件。</param>
    private void AcceptQuest(BsonDocument document, ProgressRequest request)
    {
        if (request.ObjectiveId != 0 || request.MapId != 0 || request.MapInstanceId != 0 || request.Count != 0 || request.DialogueId != 0 || request.DialogueNodeId != 0)
            throw new ProgressOperationException(ProgressError.InvalidRequest, "Unexpected fields for quest acceptance.");
        cfg.Quest.QuestDefinition quest = Quest(request.QuestId);
        BsonDocument progress = GetQuestProgress(document, request.QuestId, true);
        int status = progress["status"].ToInt32();
        if (status == QuestStatusCode.Locked)
            throw new ProgressOperationException(ProgressError.QuestLocked, "Quest is locked.");
        if (status != QuestStatusCode.Available)
            throw new ProgressOperationException(ProgressError.QuestNotActive, "Quest is not available for acceptance.");
        if (quest.Activation != cfg.Quest.Activation.AcceptFromNpc || request.SourceId != quest.AcceptNpcId)
            throw new ProgressOperationException(ProgressError.InvalidEventSource, "Quest acceptance source does not match configuration.");
        progress["status"] = QuestStatusCode.Active;
    }

    /// <summary>验证任务简报及当前地图后记录玩家确认。</summary>
    /// <param name="document">本次事务中的账号文档副本。</param>
    /// <param name="request">简报确认事件。</param>
    private void AcknowledgeBriefing(BsonDocument document, ProgressRequest request)
    {
        if (request.ObjectiveId != 0 || request.MapInstanceId != 0 || request.SourceId != 0 || request.Count != 0 || request.DialogueId != 0 || request.DialogueNodeId != 0)
            throw new ProgressOperationException(ProgressError.InvalidRequest, "Unexpected fields for briefing acknowledgement.");
        cfg.Quest.QuestDefinition quest = Quest(request.QuestId);
        BsonDocument progress = GetQuestProgress(document, request.QuestId, false);
        if (progress["status"].ToInt32() != QuestStatusCode.Active)
            throw new ProgressOperationException(ProgressError.QuestNotActive, "Briefing quest is not active.");
        if (string.IsNullOrWhiteSpace(quest.BriefingTextKey) ||
            (quest.StartMapId > 0 && request.MapId != quest.StartMapId))
            throw new ProgressOperationException(ProgressError.InvalidEventSource, "Briefing does not match current map.");
        progress["briefingAcknowledged"] = true;
    }

    /// <summary>根据对话表指定节点的配置值写入账号剧情标记。</summary>
    /// <param name="document">本次事务中的账号文档副本。</param>
    /// <param name="request">剧情标记节点事件。</param>
    private void SetDialogueFlag(BsonDocument document, ProgressRequest request)
    {
        if (request.QuestId != 0 || request.ObjectiveId != 0 || request.MapInstanceId != 0 || request.SourceId != 0 || request.Count != 0 || request.DialogueId <= 0)
            throw new ProgressOperationException(ProgressError.InvalidRequest, "Unexpected fields for dialogue flag event.");
        var nodes = Table<cfg.Dialogue.TbDialogueNode>(cfg.Tables.dialogue_tbdialoguenode);
        cfg.Dialogue.DialogueNode? node = nodes.GetOrDefault(request.DialogueNodeId);
        if (node == null || node.DialogueId != request.DialogueId || node.Type != DialogueNodeType.SetFlag || string.IsNullOrWhiteSpace(node.FlagKey))
            throw new ProgressOperationException(ProgressError.InvalidEventSource, "Node is not a configured SetFlag node.");
        SetStoryFlag(document, node.FlagKey, node.FlagValue);
    }

    /// <summary>验证目标阶段和事件来源后推进目标并完成任务奖励。</summary>
    /// <param name="document">本次事务中的账号文档副本。</param>
    /// <param name="request">单次目标操作事件。</param>
    private void ApplyObjective(BsonDocument document, ProgressRequest request)
    {
        if (request.DialogueNodeId != 0)
            throw new ProgressOperationException(ProgressError.InvalidRequest, "Unexpected dialogue node for objective event.");
        cfg.Quest.QuestDefinition quest = Quest(request.QuestId);
        BsonDocument questProgress = GetQuestProgress(document, request.QuestId, false);
        if (questProgress["status"].ToInt32() != QuestStatusCode.Active)
            throw new ProgressOperationException(ProgressError.QuestNotActive, "Quest is not active.");
        cfg.Quest.QuestObjective objective = Objective(request.ObjectiveId);
        cfg.Quest.QuestObjectiveGroup group = Group(objective.GroupId);
        if (group.QuestId != quest.Id)
            throw new ProgressOperationException(ProgressError.InvalidContentConfig, "Objective belongs to another quest.");
        cfg.Quest.QuestObjectiveGroup? currentGroup = CurrentGroup(quest.Id, document);
        if (currentGroup == null || currentGroup.Id != group.Id)
            throw new ProgressOperationException(ProgressError.InvalidObjectiveOrder, "Objective group is not currently open.");
        int amount = ValidateObjectiveSource(quest, objective, request);
        BsonDocument currentObjective = GetObjectiveProgress(questProgress, objective.Id, true);
        int oldCount = currentObjective["count"].ToInt32();
        if (oldCount >= objective.TargetCount) return;
        int appliedAmount = Math.Min(amount, objective.TargetCount - oldCount);
        if (objective.Type == cfg.Quest.ObjectiveType.CollectItem) AddItem(document, objective.ItemId, appliedAmount);
        currentObjective["count"] = checked(oldCount + appliedAmount);
        if (IsQuestComplete(quest.Id, document))
        {
            questProgress["status"] = QuestStatusCode.Completed;
            GrantRewards(document, quest.Id);
        }
    }

    /// <summary>按目标类型验证请求来源并返回可计入进度的数量。</summary>
    /// <param name="quest">当前目标所属的任务配置。</param>
    /// <param name="objective">需要推进的任务目标配置。</param>
    /// <param name="request">客户端提交的来源地图和实体编号。</param>
    /// <returns>该事件可计入目标进度的数量。</returns>
    private int ValidateObjectiveSource(cfg.Quest.QuestDefinition quest, cfg.Quest.QuestObjective objective, ProgressRequest request)
    {
        if (request.Count != 1)
            throw new ProgressOperationException(ProgressError.InvalidRequest, "Objective events must report one action.");
        switch (objective.Type)
        {
            case cfg.Quest.ObjectiveType.InspectObject:
            case cfg.Quest.ObjectiveType.CollectItem:
            {
                if (request.MapId != objective.SourceMapId || request.MapInstanceId != objective.SourceInstanceId || request.SourceId <= 0)
                    throw new ProgressOperationException(ProgressError.InvalidEventSource, "Map objective source does not match.");
                cfg.Map.MapData? map = Table<cfg.Map.TbMapData>(cfg.Tables.map_tbmapdata).GetOrDefault(request.MapId);
                cfg.Map.ObjectPlacement? placement = map?.Objects.FirstOrDefault(value => value.InstanceId == request.MapInstanceId);
                cfg.Map.MapObject? definition = placement == null ? null : Table<cfg.Map.TbMapObject>(cfg.Tables.map_tbmapobject).GetOrDefault(placement.ObjectId);
                if (placement == null || placement.ObjectiveId != objective.Id || placement.ObjectId != request.SourceId ||
                    definition == null || definition.Type != cfg.Map.ObjectType.TaskMarker)
                    throw new ProgressOperationException(ProgressError.InvalidEventSource, "Map marker is not bound to this objective.");
                if (objective.Type == cfg.Quest.ObjectiveType.InspectObject)
                {
                    if (objective.DialogueId > 0 && request.DialogueId != objective.DialogueId)
                        throw new ProgressOperationException(ProgressError.InvalidEventSource, "Inspection feedback dialogue does not match.");
                    return 1;
                }
                if (objective.ItemId <= 0 || objective.CollectCount <= 0)
                    throw new ProgressOperationException(ProgressError.InvalidContentConfig, "Collection objective is missing item configuration.");
                return objective.CollectCount;
            }
            case cfg.Quest.ObjectiveType.CompleteDialogue:
                if (request.MapId != objective.SourceMapId || request.MapInstanceId != 0 ||
                    request.SourceId != objective.SourceNpcId || request.DialogueId != objective.DialogueId)
                    throw new ProgressOperationException(ProgressError.InvalidEventSource, "Dialogue objective source does not match.");
                cfg.Map.Npc? npc = Table<cfg.Map.TbNpc>(cfg.Tables.map_tbnpc).GetOrDefault(request.SourceId);
                if (npc == null || npc.DialogueId != request.DialogueId)
                    throw new ProgressOperationException(ProgressError.InvalidEventSource, "NPC dialogue does not match configuration.");
                return 1;
            case cfg.Quest.ObjectiveType.KillEnemy:
                if (request.SourceId != objective.EnemyId || (objective.SourceMapId > 0 && request.MapId != objective.SourceMapId))
                    throw new ProgressOperationException(ProgressError.InvalidEventSource, "Enemy objective source does not match.");
                if (Table<cfg.Entity.TbMonster>(cfg.Tables.entity_tbmonster).GetOrDefault(request.SourceId) == null)
                    throw new ProgressOperationException(ProgressError.InvalidContentConfig, "Configured enemy does not exist.");
                return 1;
            default:
                throw new ProgressOperationException(ProgressError.InvalidContentConfig, "Unsupported objective type.");
        }
    }

    /// <summary>验证奖励配置并在账号文档中发放一次性道具奖励。</summary>
    /// <param name="document">本次事务中的账号文档副本。</param>
    /// <param name="questId">刚完成的任务编号。</param>
    private void GrantRewards(BsonDocument document, int questId)
    {
        BsonArray receipts = document["rewardReceipts"].AsBsonArray;
        if (receipts.Any(value => value.ToInt32() == questId)) return;
        var rewards = Table<TbQuestReward>(cfg.Tables.quest_tbquestreward).DataList
            .Where(reward => reward.QuestId == questId).OrderBy(reward => reward.Order).ToArray();
        foreach (cfg.Quest.QuestReward reward in rewards)
        {
            if (Table<ItemTables.TbItem>(cfg.Tables.item_tbitem).GetOrDefault(reward.ItemId) == null || reward.Count <= 0)
                throw new ProgressOperationException(ProgressError.InvalidContentConfig, "Quest reward configuration is invalid.");
            AddItem(document, reward.ItemId, reward.Count);
        }
        receipts.Add(questId);
    }

    /// <summary>根据前置任务和剧情标记更新任务开放状态。</summary>
    /// <param name="document">本次读取或提交过程中的账号文档副本。</param>
    /// <param name="mapId">本次加载或变更请求所在地图编号。</param>
    /// <returns>文档中是否有任务状态发生变化。</returns>
    private bool RefreshAvailability(BsonDocument document, int mapId)
    {
        bool changed = false;
        foreach (cfg.Quest.QuestDefinition quest in Table<TbQuestDefinition>(cfg.Tables.quest_tbquestdefinition).DataList.OrderBy(value => value.SortOrder))
        {
            BsonDocument progress = GetQuestProgress(document, quest.Id, false);
            if (progress["status"].ToInt32() != QuestStatusCode.Locked) continue;
            bool prerequisiteComplete = quest.PrerequisiteQuestId == 0 || GetStatus(document, quest.PrerequisiteQuestId) == QuestStatusCode.Completed;
            bool flagSatisfied = string.IsNullOrWhiteSpace(quest.PrerequisiteStoryFlagKey) ||
                                 GetStoryFlag(document, quest.PrerequisiteStoryFlagKey) == quest.PrerequisiteStoryFlagValue;
            if (!prerequisiteComplete || !flagSatisfied) continue;
            int status = quest.Activation == cfg.Quest.Activation.AutoAfterPrerequisites
                ? QuestStatusCode.Active : QuestStatusCode.Available;
            progress = GetQuestProgress(document, quest.Id, true);
            progress["status"] = status;
            changed = true;
        }
        foreach (cfg.Quest.QuestDefinition quest in Table<TbQuestDefinition>(cfg.Tables.quest_tbquestdefinition).DataList)
        {
            if (quest.Activation != cfg.Quest.Activation.AutoOnMapEnter || quest.StartMapId != mapId) continue;
            BsonDocument progress = GetQuestProgress(document, quest.Id, false);
            if (progress["status"].ToInt32() == QuestStatusCode.Available)
            {
                progress["status"] = QuestStatusCode.Active;
                changed = true;
            }
        }
        return changed;
    }

    /// <summary>检查任务的所有阶段是否达到配置要求。</summary>
    /// <param name="questId">需要检查的任务编号。</param>
    /// <param name="document">包含目标进度的账号文档副本。</param>
    /// <returns>所有阶段均满足要求时返回真。</returns>
    private bool IsQuestComplete(int questId, BsonDocument document)
    {
        var questProgress = GetQuestProgress(document, questId, false);
        foreach (cfg.Quest.QuestObjectiveGroup group in Groups(questId))
        {
            int completed = Objectives(group.Id).Count(objective => GetObjectiveCount(questProgress, objective.Id) >= objective.TargetCount);
            if (completed < group.RequiredObjectiveCount) return false;
        }
        return true;
    }

    /// <summary>查找任务中按阶段顺序最早的未完成目标组。</summary>
    /// <param name="questId">需要检查的任务编号。</param>
    /// <param name="document">包含目标进度的账号文档副本。</param>
    /// <returns>当前开放的目标组；全部完成时返回空值。</returns>
    private cfg.Quest.QuestObjectiveGroup? CurrentGroup(int questId, BsonDocument document)
    {
        BsonDocument progress = GetQuestProgress(document, questId, false);
        foreach (cfg.Quest.QuestObjectiveGroup group in Groups(questId))
        {
            int completed = Objectives(group.Id).Count(objective => GetObjectiveCount(progress, objective.Id) >= objective.TargetCount);
            if (completed < group.RequiredObjectiveCount) return group;
        }
        return null;
    }

    /// <summary>按配置顺序读取指定任务的所有目标组。</summary>
    /// <param name="questId">任务配置编号。</param>
    /// <returns>按阶段顺序排列的目标组。</returns>
    private IEnumerable<cfg.Quest.QuestObjectiveGroup> Groups(int questId) =>
        Table<TbQuestObjectiveGroup>(cfg.Tables.quest_tbquestobjectivegroup).DataList
            .Where(group => group.QuestId == questId).OrderBy(group => group.Order);

    /// <summary>读取指定目标组包含的所有目标。</summary>
    /// <param name="groupId">目标组配置编号。</param>
    /// <returns>属于指定组的目标配置。</returns>
    private IEnumerable<cfg.Quest.QuestObjective> Objectives(int groupId) =>
        Table<TbQuestObjective>(cfg.Tables.quest_tbquestobjective).DataList.Where(objective => objective.GroupId == groupId);

    /// <summary>读取任务配置；配置缺失时返回可映射到 RPC 的异常。</summary>
    /// <param name="questId">任务配置编号。</param>
    /// <returns>匹配的任务定义。</returns>
    private cfg.Quest.QuestDefinition Quest(int questId) => Table<TbQuestDefinition>(cfg.Tables.quest_tbquestdefinition).GetOrDefault(questId)
        ?? throw new ProgressOperationException(ProgressError.InvalidContentConfig, "Quest configuration is missing.");

    /// <summary>读取目标配置；配置缺失时返回可映射到 RPC 的异常。</summary>
    /// <param name="objectiveId">目标配置编号。</param>
    /// <returns>匹配的目标定义。</returns>
    private cfg.Quest.QuestObjective Objective(int objectiveId) => Table<TbQuestObjective>(cfg.Tables.quest_tbquestobjective).GetOrDefault(objectiveId)
        ?? throw new ProgressOperationException(ProgressError.InvalidContentConfig, "Objective configuration is missing.");

    /// <summary>读取目标组配置；配置缺失时返回可映射到 RPC 的异常。</summary>
    /// <param name="groupId">目标组配置编号。</param>
    /// <returns>匹配的目标组定义。</returns>
    private cfg.Quest.QuestObjectiveGroup Group(int groupId) => Table<TbQuestObjectiveGroup>(cfg.Tables.quest_tbquestobjectivegroup).GetOrDefault(groupId)
        ?? throw new ProgressOperationException(ProgressError.InvalidContentConfig, "Objective group configuration is missing.");

    /// <summary>按表名和生成表类型取得服务器配置对象。</summary>
    /// <typeparam name="TTable">预期的 Luban 表类型。</typeparam>
    /// <param name="name">Luban 表注册名。</param>
    /// <returns>类型匹配的表实例。</returns>
    private TTable Table<TTable>(string name) where TTable : class, cfg.Tables.IVOFun
    {
        if (!_tables.CfgDic.TryGetValue(name, out cfg.Tables.IVOFun? table) || table is not TTable typed)
            throw new ProgressOperationException(ProgressError.InvalidContentConfig, $"Missing configuration table {name}.");
        return typed;
    }

    /// <summary>构造一个字段完整、尚未保存的空账号进度文档。</summary>
    /// <param name="playerId">作为 MongoDB 主键的已认证玩家编号。</param>
    /// <returns>可写入进度集合的新文档。</returns>
    private static BsonDocument NewDocument(int playerId) => new()
    {
        {"_id", playerId}, {"schemaVersion", SchemaVersion}, {"revision", 0L}, {"updatedAtUtc", DateTime.UtcNow},
        {"quests", new BsonArray()}, {"storyFlags", new BsonArray()}, {"items", new BsonArray()},
        {"rewardReceipts", new BsonArray()}, {"recentRequests", new BsonArray()}
    };

    /// <summary>检查账号文档的 schema 版本和必需集合字段。</summary>
    /// <param name="document">从 MongoDB 读取的进度文档。</param>
    private static void ValidateDocument(BsonDocument document)
    {
        if (!document.TryGetValue("schemaVersion", out var schema) || schema.ToInt32() != SchemaVersion ||
            !document.TryGetValue("revision", out var revision) || revision.ToInt64() < 0)
            throw new ProgressOperationException(ProgressError.InvalidContentConfig, "Unknown player progress document schema.");
        foreach (string name in new[] { "quests", "storyFlags", "items", "rewardReceipts", "recentRequests" })
            if (!document.TryGetValue(name, out var value) || !value.IsBsonArray)
                throw new ProgressOperationException(ProgressError.InvalidContentConfig, "Malformed player progress document.");
    }

    /// <summary>查询任务进度；按需创建默认的未开放进度。</summary>
    /// <param name="document">账号进度文档。</param>
    /// <param name="questId">任务编号。</param>
    /// <param name="create">未找到时是否将默认进度写入文档副本。</param>
    /// <returns>已有或新建的任务进度子文档。</returns>
    private static BsonDocument GetQuestProgress(BsonDocument document, int questId, bool create)
    {
        BsonArray quests = document["quests"].AsBsonArray;
        foreach (BsonDocument value in quests.Select(item => item.AsBsonDocument))
            if (value["questId"].ToInt32() == questId) return value;
        var result = new BsonDocument { {"questId", questId}, {"status", QuestStatusCode.Locked},
            {"briefingAcknowledged", false}, {"objectives", new BsonArray()} };
        if (create) quests.Add(result);
        return result;
    }

    /// <summary>读取任务状态；没有对应进度时返回未开放状态。</summary>
    /// <param name="document">账号进度文档。</param>
    /// <param name="questId">任务编号。</param>
    /// <returns>持久化的任务状态编号。</returns>
    private static int GetStatus(BsonDocument document, int questId) => GetQuestProgress(document, questId, false)["status"].ToInt32();

    /// <summary>查询目标进度；按需创建数量为零的默认记录。</summary>
    /// <param name="questProgress">所属任务进度子文档。</param>
    /// <param name="objectiveId">目标编号。</param>
    /// <param name="create">未找到时是否将默认进度写入文档副本。</param>
    /// <returns>已有或新建的目标进度子文档。</returns>
    private static BsonDocument GetObjectiveProgress(BsonDocument questProgress, int objectiveId, bool create)
    {
        BsonArray objectives = questProgress["objectives"].AsBsonArray;
        foreach (BsonDocument value in objectives.Select(item => item.AsBsonDocument))
            if (value["objectiveId"].ToInt32() == objectiveId) return value;
        var result = new BsonDocument { {"objectiveId", objectiveId}, {"count", 0} };
        if (create) objectives.Add(result);
        return result;
    }

    /// <summary>读取目标数量；没有对应记录时返回零。</summary>
    /// <param name="questProgress">所属任务进度子文档。</param>
    /// <param name="objectiveId">目标编号。</param>
    /// <returns>已保存的目标完成数量。</returns>
    private static int GetObjectiveCount(BsonDocument questProgress, int objectiveId) =>
        GetObjectiveProgress(questProgress, objectiveId, false)["count"].ToInt32();

    /// <summary>查询账号剧情标记并返回其是否存在。</summary>
    /// <param name="document">账号进度文档。</param>
    /// <param name="key">剧情标记稳定键。</param>
    /// <param name="value">找到标记时输出其布尔值；缺失时输出 false。</param>
    /// <returns>该标记存在时返回真。</returns>
    private static bool TryGetStoryFlag(BsonDocument document, string key, out bool value)
    {
        foreach (BsonDocument item in document["storyFlags"].AsBsonArray.Select(entry => entry.AsBsonDocument))
            if (item["key"].AsString == key) { value = item["value"].ToBoolean(); return true; }
        value = false;
        return false;
    }

    /// <summary>读取剧情标记；未设置的标记按 false 处理。</summary>
    /// <param name="document">账号进度文档。</param>
    /// <param name="key">剧情标记稳定键。</param>
    /// <returns>标记当前值。</returns>
    private static bool GetStoryFlag(BsonDocument document, string key)
    {
        return TryGetStoryFlag(document, key, out bool value) && value;
    }

    /// <summary>更新已存在的剧情标记或追加新的标记值。</summary>
    /// <param name="document">账号进度文档副本。</param>
    /// <param name="key">剧情标记稳定键。</param>
    /// <param name="value">从对话表读取的目标值。</param>
    private static void SetStoryFlag(BsonDocument document, string key, bool value)
    {
        foreach (BsonDocument item in document["storyFlags"].AsBsonArray.Select(entry => entry.AsBsonDocument))
            if (item["key"].AsString == key) { item["value"] = value; return; }
        document["storyFlags"].AsBsonArray.Add(new BsonDocument { {"key", key}, {"value", value} });
    }

    /// <summary>向账号道具堆叠增加指定数量，并检查整数溢出。</summary>
    /// <param name="document">账号进度文档副本。</param>
    /// <param name="itemId">道具配置编号。</param>
    /// <param name="count">需要增加的正数数量。</param>
    private static void AddItem(BsonDocument document, int itemId, int count)
    {
        if (itemId <= 0 || count <= 0) throw new ProgressOperationException(ProgressError.InvalidContentConfig, "Invalid item stack delta.");
        foreach (BsonDocument item in document["items"].AsBsonArray.Select(entry => entry.AsBsonDocument))
        {
            if (item["itemId"].ToInt32() != itemId) continue;
            int updated;
            try { updated = checked(item["count"].ToInt32() + count); }
            catch (OverflowException) { throw new ProgressOperationException(ProgressError.InvalidContentConfig, "Item stack overflow."); }
            item["count"] = updated;
            return;
        }
        document["items"].AsBsonArray.Add(new BsonDocument { {"itemId", itemId}, {"count", count} });
    }

    /// <summary>在近期请求窗口中查找相同请求编号。</summary>
    /// <param name="document">账号进度文档。</param>
    /// <param name="requestId">幂等请求编号。</param>
    /// <returns>已保存的请求记录；未找到时返回空值。</returns>
    private static BsonDocument? FindRequest(BsonDocument document, string requestId) =>
        document["recentRequests"].AsBsonArray.Select(value => value.AsBsonDocument)
            .FirstOrDefault(value => value["requestId"].AsString == requestId);

    /// <summary>保存成功请求并裁剪超出上限的最早记录。</summary>
    /// <param name="document">账号进度文档副本。</param>
    /// <param name="requestId">幂等请求编号。</param>
    /// <param name="fingerprint">用于检测编号复用的事件指纹。</param>
    private static void RememberRequest(BsonDocument document, string requestId, string fingerprint)
    {
        BsonArray recent = document["recentRequests"].AsBsonArray;
        recent.Add(new BsonDocument { {"requestId", requestId}, {"fingerprint", fingerprint} });
        while (recent.Count > MaximumRecentRequests) recent.RemoveAt(0);
    }

    /// <summary>检查请求编号和通用字段范围。</summary>
    /// <param name="request">客户端提交的进度事件。</param>
    private static void ValidateRequestShape(ProgressRequest request)
    {
        if (!Guid.TryParse(request.RequestId, out _) || request.RequestId.Length > 40)
            throw new ProgressOperationException(ProgressError.InvalidRequest, "Request id must be a GUID.");
        if (request.EventType < (int)ProgressEventType.AcceptQuest || request.EventType > (int)ProgressEventType.DialogueSetFlag ||
            request.QuestId < 0 || request.ObjectiveId < 0 || request.MapId < 0 || request.MapInstanceId < 0 || request.SourceId < 0 ||
            request.Count < 0 || request.DialogueId < 0 || request.DialogueNodeId < 0)
            throw new ProgressOperationException(ProgressError.InvalidRequest, "Event has invalid fields.");
    }

    /// <summary>将事件字段编码为确定性哈希，用于幂等请求内容校验。</summary>
    /// <param name="request">客户端提交的进度事件。</param>
    /// <returns>事件内容的 SHA-256 十六进制指纹。</returns>
    private static string Fingerprint(ProgressRequest request)
    {
        string source = string.Join("|", request.EventType, request.QuestId, request.ObjectiveId, request.MapId,
            request.MapInstanceId, request.SourceId, request.Count, request.DialogueId, request.DialogueNodeId);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
    }

    /// <summary>表示进度文档版本已被并发请求修改。</summary>
    private sealed class CasConflictException : Exception { }
}
