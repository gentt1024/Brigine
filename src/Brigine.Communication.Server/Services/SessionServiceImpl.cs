using Grpc.Core;
using Brigine.Communication.Protos;
using System.Collections.Concurrent;

namespace Brigine.Communication.Server.Services
{
    /// <summary>
    /// 会话服务实现 - 管理协作会话和用户状态
    /// </summary>
    public class SessionServiceImpl : SessionService.SessionServiceBase
    {
        private readonly ILogger<SessionServiceImpl> _logger;
        private readonly ConcurrentDictionary<string, SessionInfo> _sessions = new();
        private readonly ConcurrentDictionary<string, List<UserInfo>> _sessionUsers = new();
        private readonly ConcurrentDictionary<string, List<IServerStreamWriter<SessionEvent>>> _sessionEventStreams = new();

        public SessionServiceImpl(ILogger<SessionServiceImpl> logger)
        {
            _logger = logger;
        }

        public override Task<CreateSessionResponse> CreateSession(CreateSessionRequest request, ServerCallContext context)
        {
            try
            {
                var sessionId = Guid.NewGuid().ToString();
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                var sessionInfo = new SessionInfo
                {
                    SessionId = sessionId,
                    ProjectName = request.ProjectName,
                    CreatorId = request.CreatorId,
                    CreatedTime = now,
                    Status = SessionStatus.Active
                };

                // 复制元数据
                foreach (var kvp in request.Metadata)
                {
                    sessionInfo.Metadata[kvp.Key] = kvp.Value;
                }

                _sessions[sessionId] = sessionInfo;
                _sessionUsers[sessionId] = new List<UserInfo>();
                _sessionEventStreams[sessionId] = new List<IServerStreamWriter<SessionEvent>>();

                _logger.LogInformation($"创建会话成功: {sessionId} (项目: {request.ProjectName})");

                return Task.FromResult(new CreateSessionResponse
                {
                    Success = true,
                    SessionId = sessionId,
                    SessionInfo = sessionInfo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建会话失败");
                return Task.FromResult(new CreateSessionResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        public override Task<JoinSessionResponse> JoinSession(JoinSessionRequest request, ServerCallContext context)
        {
            try
            {
                if (!_sessions.TryGetValue(request.SessionId, out var sessionInfo))
                {
                    return Task.FromResult(new JoinSessionResponse
                    {
                        Success = false,
                        ErrorMessage = "会话不存在"
                    });
                }

                var userInfo = new UserInfo
                {
                    UserId = request.UserId,
                    DisplayName = request.UserId, // 可以从元数据中获取显示名称
                    ClientType = request.ClientType,
                    JoinedTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Status = UserStatus.Online
                };

                // 复制客户端元数据
                foreach (var kvp in request.ClientMetadata)
                {
                    userInfo.Metadata[kvp.Key] = kvp.Value;
                }

                var users = _sessionUsers[request.SessionId];
                
                // 移除已存在的用户（重新加入）
                users.RemoveAll(u => u.UserId == request.UserId);
                users.Add(userInfo);

                _logger.LogInformation($"用户加入会话: {request.UserId} -> {request.SessionId}");

                // 广播用户加入事件
                _ = Task.Run(async () => await BroadcastSessionEvent(request.SessionId, new SessionEvent
                {
                    EventType = SessionEventType.UserJoined,
                    SessionId = request.SessionId,
                    UserId = request.UserId,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                }));

                return Task.FromResult(new JoinSessionResponse
                {
                    Success = true,
                    SessionInfo = sessionInfo,
                    ActiveUsers = { users }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加入会话失败");
                return Task.FromResult(new JoinSessionResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        public override Task<LeaveSessionResponse> LeaveSession(LeaveSessionRequest request, ServerCallContext context)
        {
            try
            {
                if (_sessionUsers.TryGetValue(request.SessionId, out var users))
                {
                    users.RemoveAll(u => u.UserId == request.UserId);
                    
                    _logger.LogInformation($"用户离开会话: {request.UserId} <- {request.SessionId}");

                    // 广播用户离开事件
                    _ = Task.Run(async () => await BroadcastSessionEvent(request.SessionId, new SessionEvent
                    {
                        EventType = SessionEventType.UserLeft,
                        SessionId = request.SessionId,
                        UserId = request.UserId,
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    }));
                }

                return Task.FromResult(new LeaveSessionResponse
                {
                    Success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "离开会话失败");
                return Task.FromResult(new LeaveSessionResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        public override Task<GetSessionInfoResponse> GetSessionInfo(GetSessionInfoRequest request, ServerCallContext context)
        {
            try
            {
                if (!_sessions.TryGetValue(request.SessionId, out var sessionInfo))
                {
                    return Task.FromResult(new GetSessionInfoResponse
                    {
                        Success = false,
                        ErrorMessage = "会话不存在"
                    });
                }

                var users = _sessionUsers.GetValueOrDefault(request.SessionId, new List<UserInfo>());

                return Task.FromResult(new GetSessionInfoResponse
                {
                    Success = true,
                    SessionInfo = sessionInfo,
                    ActiveUsers = { users }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取会话信息失败");
                return Task.FromResult(new GetSessionInfoResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        public override async Task SessionEvents(SessionEventsRequest request, IServerStreamWriter<SessionEvent> responseStream, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation($"开始会话事件流: {request.SessionId} (用户: {request.UserId})");

                // 添加到事件流列表
                if (_sessionEventStreams.TryGetValue(request.SessionId, out var streams))
                {
                    streams.Add(responseStream);
                }

                // 保持连接直到取消
                while (!context.CancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, context.CancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation($"会话事件流已取消: {request.SessionId} (用户: {request.UserId})");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "会话事件流错误");
            }
            finally
            {
                // 从事件流列表中移除
                if (_sessionEventStreams.TryGetValue(request.SessionId, out var streams))
                {
                    streams.Remove(responseStream);
                }
            }
        }

        private async Task BroadcastSessionEvent(string sessionId, SessionEvent sessionEvent)
        {
            if (!_sessionEventStreams.TryGetValue(sessionId, out var streams))
                return;

            var tasks = new List<Task>();
            var streamsToRemove = new List<IServerStreamWriter<SessionEvent>>();

            foreach (var stream in streams.ToList())
            {
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await stream.WriteAsync(sessionEvent);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "发送会话事件失败，移除流");
                        streamsToRemove.Add(stream);
                    }
                }));
            }

            await Task.WhenAll(tasks);

            // 移除失效的流
            foreach (var stream in streamsToRemove)
            {
                streams.Remove(stream);
            }
        }

        /// <summary>
        /// 获取所有活跃会话（用于管理和监控）
        /// </summary>
        public IEnumerable<SessionInfo> GetActiveSessions()
        {
            return _sessions.Values.Where(s => s.Status == SessionStatus.Active);
        }

        /// <summary>
        /// 获取会话用户数量
        /// </summary>
        public int GetSessionUserCount(string sessionId)
        {
            return _sessionUsers.GetValueOrDefault(sessionId, new List<UserInfo>()).Count;
        }
    }
} 