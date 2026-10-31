using System.Security.Cryptography;
using System.Text;
using Entity.Auth;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Hotfix.Auth;

/// <summary>可安全返回给客户端的认证失败。</summary>
public sealed class AuthFailureException : Exception
{
    /// <summary>业务错误码。</summary>
    public uint Code { get; }
    /// <summary>HTTP 状态码。</summary>
    public int Status { get; }

    /// <summary>建立一个不包含敏感凭据的认证错误。</summary>
    /// <param name="code">认证业务错误码。</param>
    /// <param name="status">HTTP 状态码。</param>
    /// <param name="message">可向玩家展示的描述。</param>
    public AuthFailureException(uint code, int status, string message) : base(message)
    {
        Code = code;
        Status = status;
    }
}

/// <summary>游客凭据、登录授权与一次性票据的 MongoDB 存储逻辑。</summary>
public sealed class GuestAuthStore
{
    /// <summary>游客集合名称，与原有玩家集合隔离。</summary>
    public const string GuestsCollection = "AuthGuests";
    /// <summary>一次性票据集合名称。</summary>
    public const string TicketsCollection = "AuthTickets";
    /// <summary>原子编号计数器集合名称。</summary>
    public const string CountersCollection = "AuthCounters";
    /// <summary>玩家编号计数器主键。</summary>
    private const string PlayerCounter = "PlayerId";
    /// <summary>游客持久化集合。</summary>
    private readonly IMongoCollection<BsonDocument> _guests;
    /// <summary>一次性票据集合。</summary>
    private readonly IMongoCollection<BsonDocument> _tickets;
    /// <summary>玩家编号计数器集合。</summary>
    private readonly IMongoCollection<BsonDocument> _counters;
    /// <summary>原有 Fantasy 玩家集合，用于检查索引和已有编号。</summary>
    private readonly IMongoCollection<BsonDocument> _players;

    /// <summary>使用当前 World 的数据库，不额外建立 MongoDB 客户端。</summary>
    /// <param name="database">当前 World 的原生数据库。</param>
    public GuestAuthStore(IMongoDatabase database)
    {
        _guests = database.GetCollection<BsonDocument>(GuestsCollection);
        _tickets = database.GetCollection<BsonDocument>(TicketsCollection);
        _counters = database.GetCollection<BsonDocument>(CountersCollection);
        _players = database.GetCollection<BsonDocument>("Player");
    }

    /// <summary>建立唯一索引和 TTL 索引，并从已有最大编号初始化计数器。</summary>
    /// <returns>初始化任务；重复玩家编号等问题向启动路径报告。</returns>
    public async Task InitializeAsync()
    {
        var uniquePlayer = new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("PlayerId"),
            new CreateIndexOptions { Unique = true, Name = "UniquePlayerId" });
        await _players.Indexes.CreateOneAsync(uniquePlayer);
        await _guests.Indexes.CreateOneAsync(uniquePlayer);
        await _guests.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("SessionHash"),
            new CreateIndexOptions { Unique = true, Sparse = true, Name = "UniqueSessionHash" }));
        await _tickets.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("ExpiresAtUtc"),
            new CreateIndexOptions { ExpireAfter = TimeSpan.Zero, Name = "ExpiredTicketCleanup" }));

        long maximum = Math.Max(await GetMaximumPlayerIdAsync(_players), await GetMaximumPlayerIdAsync(_guests));
        if (maximum >= int.MaxValue)
            throw new InvalidOperationException("现有玩家编号已超出可分配的正整数范围。");
        await _counters.UpdateOneAsync(new BsonDocument("_id", PlayerCounter),
            Builders<BsonDocument>.Update.Max("Value", maximum), new UpdateOptions { IsUpsert = true });
    }

    /// <summary>验证持久凭据，为首次访问创建游客，并替换当前登录授权。</summary>
    /// <param name="request">客户端的随机持久凭据。</param>
    /// <param name="gate">从 Fantasy 配置读取的唯一目标 Gate。</param>
    /// <returns>本次登录授权和一次性票据。</returns>
    public async Task<GuestLoginResponse> LoginAsync(GuestLoginRequest request, AuthTicketResponse gate)
    {
        ValidateCredentials(request);
        string guestId = Guid.Parse(request.GuestId).ToString("N");
        string secretHash = Hash(request.GuestSecret);
        BsonDocument? guest = await _guests.Find(new BsonDocument("_id", guestId)).FirstOrDefaultAsync();
        if (guest == null)
            guest = await CreateGuestAsync(guestId, secretHash);
        if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(guest["SecretHash"].AsString),
                Convert.FromHexString(secretHash)))
            throw new AuthFailureException(AuthConstants.InvalidCredentials, 401, "游客凭据不正确，请保留本地数据并重试。");

        string sessionToken = CreateToken();
        DateTime sessionExpires = DateTime.UtcNow.AddHours(AuthConstants.SessionLifetimeHours);
        guest = await _guests.FindOneAndUpdateAsync(
            new BsonDocument { { "_id", guestId }, { "SecretHash", secretHash } },
            Builders<BsonDocument>.Update.Set("SessionHash", Hash(sessionToken))
                .Set("SessionExpiresAtUtc", sessionExpires).Set("GateId", (long)gate.GateId),
            new FindOneAndUpdateOptions<BsonDocument> { ReturnDocument = ReturnDocument.After });
        var response = new GuestLoginResponse { SessionToken = sessionToken, SessionExpiresAtUtc = sessionExpires };
        await FillTicketAsync(guest, gate, response);
        return response;
    }

    /// <summary>仅使用当前未过期的登录授权签发重连票据，不重新建立登录授权。</summary>
    /// <param name="sessionToken">HTTP Bearer 授权。</param>
    /// <param name="gate">配置中的目标 Gate，须与授权绑定的 Gate 一致。</param>
    /// <returns>新的连接信息和一次性票据。</returns>
    public async Task<AuthTicketResponse> RefreshTicketAsync(string sessionToken, AuthTicketResponse gate)
    {
        ValidateToken(sessionToken, AuthConstants.SessionReplaced);
        BsonDocument? guest = await _guests.Find(new BsonDocument("SessionHash", Hash(sessionToken))).FirstOrDefaultAsync();
        if (guest == null)
            throw new AuthFailureException(AuthConstants.SessionReplaced, 401, "登录已失效或已在其他客户端登录，请重新登录。");
        if (guest["SessionExpiresAtUtc"].ToUniversalTime() <= DateTime.UtcNow)
            throw new AuthFailureException(AuthConstants.SessionExpired, 401, "登录授权已过期，请重新登录。");
        if (guest["GateId"].ToInt64() != gate.GateId)
            throw new AuthFailureException(AuthConstants.Unavailable, 503, "原游戏服务器暂不可用。");
        var response = new AuthTicketResponse();
        await FillTicketAsync(guest, gate, response);
        return response;
    }

    /// <summary>原子消费与目标 Gate 绑定且未过期的票据，然后验证授权未被替换。</summary>
    /// <param name="ticket">游戏客户端提交的一次性票据。</param>
    /// <param name="gateId">实际接收连接的 Gate 配置编号。</param>
    /// <returns>服务端确认的游客身份。</returns>
    public async Task<VerifiedGuest> ConsumeTicketAsync(string ticket, uint gateId)
    {
        ValidateToken(ticket, AuthConstants.InvalidTicket);
        var filter = Builders<BsonDocument>.Filter.Eq("_id", Hash(ticket)) &
                     Builders<BsonDocument>.Filter.Eq("GateId", (long)gateId) &
                     Builders<BsonDocument>.Filter.Gt("ExpiresAtUtc", DateTime.UtcNow);
        var record = await _tickets.FindOneAndDeleteAsync(filter);
        if (record == null)
            throw new AuthFailureException(AuthConstants.InvalidTicket, 401, "登录票据无效、已过期或已经使用。");
        var guest = new VerifiedGuest
        {
            GuestId = record["GuestId"].AsString,
            PlayerId = record["PlayerId"].AsInt32,
            PlayerName = record["PlayerName"].AsString,
            SessionHash = record["SessionHash"].AsString
        };
        await ValidateGrantAsync(guest);
        return guest;
    }

    /// <summary>在 Gate 玩家锁内再次检查授权，阻止排队中的旧登录覆盖新登录。</summary>
    /// <param name="identity">票据得到的游客身份。</param>
    /// <returns>授权检查任务。</returns>
    public async Task ValidateGrantAsync(VerifiedGuest identity)
    {
        var filter = new BsonDocument { { "_id", identity.GuestId }, { "PlayerId", identity.PlayerId },
            { "SessionHash", identity.SessionHash }, { "SessionExpiresAtUtc", new BsonDocument("$gt", DateTime.UtcNow) } };
        if (await _guests.Find(filter).FirstOrDefaultAsync() == null)
            throw new AuthFailureException(AuthConstants.SessionReplaced, 401, "登录授权已失效，请重新登录。");
    }

    /// <summary>创建游客；并发首次请求使用相同主键时读取已经创建的记录。</summary>
    /// <param name="guestId">规范化的 GUID 主键。</param>
    /// <param name="secretHash">随机密钥哈希。</param>
    /// <returns>唯一的持久游客记录。</returns>
    private async Task<BsonDocument> CreateGuestAsync(string guestId, string secretHash)
    {
        var counter = await _counters.FindOneAndUpdateAsync(
            Builders<BsonDocument>.Filter.Eq("_id", PlayerCounter) & Builders<BsonDocument>.Filter.Lt("Value", int.MaxValue),
            Builders<BsonDocument>.Update.Inc("Value", 1L),
            new FindOneAndUpdateOptions<BsonDocument> { ReturnDocument = ReturnDocument.After });
        if (counter == null)
            throw new AuthFailureException(AuthConstants.Unavailable, 503, "玩家编号暂不可分配。");
        int playerId = checked((int)counter["Value"].ToInt64());
        var guest = new BsonDocument { { "_id", guestId }, { "SecretHash", secretHash }, { "PlayerId", playerId },
            { "PlayerName", $"游客{playerId}" }, { "CreatedAtUtc", DateTime.UtcNow } };
        try { await _guests.InsertOneAsync(guest); }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return await _guests.Find(new BsonDocument("_id", guestId)).FirstOrDefaultAsync()
                ?? throw new InvalidOperationException("游客编号存在数据冲突。");
        }
        return guest;
    }

    /// <summary>持久化票据哈希，并填充客户端需要的连接资料。</summary>
    /// <param name="guest">已经验证的游客及其当前授权。</param>
    /// <param name="gate">目标 Gate 配置。</param>
    /// <param name="response">待填充的响应。</param>
    /// <returns>签发任务。</returns>
    private async Task FillTicketAsync(BsonDocument guest, AuthTicketResponse gate, AuthTicketResponse response)
    {
        string ticket = CreateToken();
        DateTime expires = DateTime.UtcNow.AddSeconds(AuthConstants.TicketLifetimeSeconds);
        await _tickets.InsertOneAsync(new BsonDocument { { "_id", Hash(ticket) }, { "GuestId", guest["_id"] },
            { "PlayerId", guest["PlayerId"] }, { "PlayerName", guest["PlayerName"] },
            { "SessionHash", guest["SessionHash"] }, { "GateId", (long)gate.GateId }, { "ExpiresAtUtc", expires } });
        response.GateId = gate.GateId;
        response.Host = gate.Host;
        response.Port = gate.Port;
        response.PlayerId = guest["PlayerId"].AsInt32;
        response.PlayerName = guest["PlayerName"].AsString;
        response.Ticket = ticket;
        response.ExpiresAtUtc = expires;
    }

    /// <summary>读取已有集合中的最大正整数玩家编号。</summary>
    /// <param name="collection">原玩家或游客集合。</param>
    /// <returns>没有记录时返回零。</returns>
    private static async Task<long> GetMaximumPlayerIdAsync(IMongoCollection<BsonDocument> collection)
    {
        var maximum = await collection.Find(Builders<BsonDocument>.Filter.Gt("PlayerId", 0))
            .Sort(Builders<BsonDocument>.Sort.Descending("PlayerId")).Limit(1).FirstOrDefaultAsync();
        return maximum == null ? 0 : maximum["PlayerId"].ToInt64();
    }

    /// <summary>验证游客 GUID 与 Base64 编码的随机密钥格式。</summary>
    /// <param name="request">HTTP 请求对象。</param>
    private static void ValidateCredentials(GuestLoginRequest request)
    {
        bool valid = request != null && Guid.TryParse(request.GuestId, out Guid id) && id != Guid.Empty;
        try { valid &= request?.GuestSecret?.Length == 44 && Convert.FromBase64String(request.GuestSecret).Length == AuthConstants.SecretByteCount; }
        catch (FormatException) { valid = false; }
        if (!valid)
            throw new AuthFailureException(AuthConstants.InvalidRequest, 400, "游客凭据格式不正确。");
    }

    /// <summary>验证固定长度的 URL 安全随机令牌，阻止空值及超长输入。</summary>
    /// <param name="token">客户端令牌。</param>
    /// <param name="errorCode">令牌无效时的业务错误码。</param>
    private static void ValidateToken(string token, uint errorCode)
    {
        if (token?.Length != 43 || token.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-' && character != '_'))
            throw new AuthFailureException(errorCode, 401, "登录凭据无效，请重新登录。");
    }

    /// <summary>生成 256 位 URL 安全随机令牌。</summary>
    /// <returns>无填充的 Base64Url 字符串。</returns>
    private static string CreateToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(AuthConstants.SecretByteCount))
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>对高熵随机凭据计算 SHA-256，数据库不保存明文。</summary>
    /// <param name="value">需要散列的随机凭据。</param>
    /// <returns>十六进制哈希。</returns>
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
