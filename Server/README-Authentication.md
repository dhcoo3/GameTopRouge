# 游客登录认证

第一版使用现有 `LoginHttp` Scene（HTTP 20001）和 `Gate` Scene（KCP 20000）。游客首次访问自动建号，后续使用本地随机凭据复用玩家；账号密码登录尚未开放。

## 启动

1. 启动本地 MongoDB；连接和数据库配置位于 `Entity/Fantasy.config`。
2. 在工程根目录运行 `dotnet run --project Server/Main/Main.csproj -- --m Develop`。
3. 日志出现两个 Scene 的“认证存储就绪”后，在 Unity 点击“游客登录”。HTTP 认证和 Gate 验票都完成后进入当前单机主线。

客户端地址由 `AppConfigs.AuthBaseUrl` 配置，开发默认 `http://127.0.0.1:20001`。外部设备需要设置可达的 HTTP 地址及 `Fantasy.config` 的监听／对外地址，开放 Gate 的 UDP 端口；部署时通过 HTTPS 入口访问认证接口。

客户端认证归 Login 模块管理：`LoginModel` 持久化游客凭据并保存内存授权、Gate 路由、Gate 确认的玩家资料和登录状态；`LoginPanel` 直接处理游客按钮并调用 `LoginController`，同时订阅登录状态事件更新控件；`LoginController` 调用 HTTP 客户端、处理 Gate 登录协议并编排重连；`LoginView` 只负责提示等展示能力，不中转按钮输入或协议调用。`GameNetworkComponent` 只管理 Fantasy Scene、KCP Session、心跳和传输断开回调。房间 RPC 从 `UIToGamePlayerBridge` 调用当前 Session。

## HTTP 契约

- `POST /api/auth/guest/login`，JSON：`guestId`（随机 GUID）、`guestSecret`（32 字节安全随机数的 Base64）。凭据由客户端首次生成，保存在 `Auth.GuestCredentials.v1`，保存失败时不会请求建号。
- 成功返回：`playerId`、`playerName`、`sessionToken`、`sessionExpiresAtUtc`、`gateId`、`host`、`port`、`protocol`、`ticket`、`expiresAtUtc`。
- `POST /api/auth/ticket`，请求头 `Authorization: Bearer <sessionToken>`，返回新的连接信息和一次性票据。
- 错误返回 `code` 和 `message`。400 表示格式不正确，401 表示身份／授权失效，503 表示服务尚未就绪或临时故障。

当前授权只保存在客户端内存中，24 小时后失效；票据 60 秒后失效且只能使用一次。自动重连沿用当前授权申请票据，不会使用长期游客密钥重新抢占登录。被踢出或重连 5 次失败后返回首页，手动登录才能建立新授权。

## 存储与兼容

新增集合 `AuthGuests`、`AuthTickets` 和 `AuthCounters`。长期密钥、当前授权和票据均只保存 SHA-256 哈希；实际秘密是 256 位随机数，不是用户密码。TTL 索引只负责清理，验票始终显式比较服务端 UTC 时间。

玩家编号保持正整数，计数器从原 `Player` 与游客集合的最大编号之后开始。已有 `Player` 数据保留；若有重复 `PlayerId`，唯一索引建立失败，服务保持不可认证并记录诊断，需先修复重复数据。

旧协议字段 `C2G_LoginRequest.Id` 保留编号但不参与认证。新客户端提交 `Ticket`；新服务端拒绝没有票据的旧客户端。因此需要一起更新客户端和服务端。

协议生成使用 `bash Tools/ProtocolExportTool/Run.sh`（Windows 使用 `Run.bat`），要求 .NET 和 Python 3。`export_protocol.py` 调用原导出器并从 proto 源注释补充两端 XML 注释，避免手工维护生成代码。

## 验收

在工程根目录执行：

```sh
python3 Tools/verify_hotfix_compile.py
```


`verify_hotfix_compile.py` 使用已导入工程的 Unity 引用与分析器编译全部热更源码，不代替编辑器运行验证。编辑器中还需验证按钮重复点击、关闭面板、断线重连耗尽和再次进入主线；无编辑器连接时这些运行场景不会自动执行。
