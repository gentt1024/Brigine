using Grpc.Core;
using Brigine.Communication.Protos;
using System.Collections.Concurrent;

namespace Brigine.Communication.Server.Services
{
    /// <summary>
    /// 事件流服务实现 - 管理实时场景变更事件
    /// </summary>
    public class EventStreamServiceImpl : EventStreamService.EventStreamServiceBase
    {
        private readonly ILogger<EventStreamServiceImpl> _logger;
        private readonly ConcurrentDictionary<string, List<EventSubscription>> _sessionSubscriptions = new();
        private readonly ConcurrentDictionary<string, List<SceneChangeEvent>> _eventHistory = new();
        private readonly int _maxHistorySize = 1000;

        public EventStreamServiceImpl(ILogger<EventStreamServiceImpl> logger)
        {
            _logger = logger;
        }

        public override async Task SubscribeEvents(SubscribeEventsRequest request, IServerStreamWriter<SceneChangeEvent> responseStream, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation($"开始场景事件订阅: {request.SessionId} (用户: {request.UserId})");

                var subscription = new EventSubscription
                {
                    SessionId = request.SessionId,
                    UserId = request.UserId,
                    Stream = responseStream,
                    EventTypes = request.EventTypes.ToHashSet(),
                    Filter = request.Filter
                };

                // 添加到订阅列表
                if (!_sessionSubscriptions.ContainsKey(request.SessionId))
                {
                    _sessionSubscriptions[request.SessionId] = new List<EventSubscription>();
                }

                _sessionSubscriptions[request.SessionId].Add(subscription);

                // 保持连接直到取消
                while (!context.CancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, context.CancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation($"场景事件订阅已取消: {request.SessionId} (用户: {request.UserId})");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "场景事件订阅错误");
            }
            finally
            {
                // 从订阅列表中移除
                if (_sessionSubscriptions.TryGetValue(request.SessionId, out var subscriptions))
                {
                    subscriptions.RemoveAll(s => s.UserId == request.UserId && s.Stream == responseStream);
                }
            }
        }

        public override async Task<PublishEventResponse> PublishEvent(PublishEventRequest request, ServerCallContext context)
        {
            try
            {
                var sceneEvent = request.Event;
                
                // 设置时间戳
                if (sceneEvent.Timestamp == 0)
                {
                    sceneEvent.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                }

                // 添加到历史记录
                AddToHistory(request.SessionId, sceneEvent);

                // 广播给订阅者
                await BroadcastEvent(request.SessionId, sceneEvent);

                _logger.LogInformation($"发布场景事件: {sceneEvent.ChangeType} | 实体: {sceneEvent.EntityId} | 用户: {request.UserId}");

                return new PublishEventResponse
                {
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布场景事件失败");
                return new PublishEventResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public override Task<GetEventHistoryResponse> GetEventHistory(GetEventHistoryRequest request, ServerCallContext context)
        {
            try
            {
                if (!_eventHistory.TryGetValue(request.SessionId, out var events))
                {
                    return Task.FromResult(new GetEventHistoryResponse
                    {
                        Success = true,
                        TotalCount = 0
                    });
                }

                var filteredEvents = events.AsEnumerable();

                // 按时间范围过滤
                if (request.StartTime > 0)
                {
                    filteredEvents = filteredEvents.Where(e => e.Timestamp >= request.StartTime);
                }

                if (request.EndTime > 0)
                {
                    filteredEvents = filteredEvents.Where(e => e.Timestamp <= request.EndTime);
                }

                // 按事件类型过滤
                if (request.EventTypes.Count > 0)
                {
                    var typeSet = request.EventTypes.ToHashSet();
                    filteredEvents = filteredEvents.Where(e => typeSet.Contains(e.ChangeType));
                }

                var totalCount = filteredEvents.Count();

                // 分页
                if (request.Offset > 0)
                {
                    filteredEvents = filteredEvents.Skip(request.Offset);
                }

                if (request.Limit > 0)
                {
                    filteredEvents = filteredEvents.Take(request.Limit);
                }

                return Task.FromResult(new GetEventHistoryResponse
                {
                    Success = true,
                    Events = { filteredEvents },
                    TotalCount = totalCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取事件历史失败");
                return Task.FromResult(new GetEventHistoryResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        private void AddToHistory(string sessionId, SceneChangeEvent sceneEvent)
        {
            if (!_eventHistory.ContainsKey(sessionId))
            {
                _eventHistory[sessionId] = new List<SceneChangeEvent>();
            }

            var history = _eventHistory[sessionId];
            history.Add(sceneEvent);

            // 限制历史记录大小
            if (history.Count > _maxHistorySize)
            {
                history.RemoveRange(0, history.Count - _maxHistorySize);
            }
        }

        private async Task BroadcastEvent(string sessionId, SceneChangeEvent sceneEvent)
        {
            if (!_sessionSubscriptions.TryGetValue(sessionId, out var subscriptions))
                return;

            var tasks = new List<Task>();
            var subscriptionsToRemove = new List<EventSubscription>();

            foreach (var subscription in subscriptions.ToList())
            {
                // 检查事件类型过滤
                if (subscription.EventTypes.Count > 0 && !subscription.EventTypes.Contains(sceneEvent.ChangeType))
                {
                    continue;
                }

                // 检查其他过滤条件
                if (!ShouldSendEvent(subscription, sceneEvent))
                {
                    continue;
                }

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await subscription.Stream.WriteAsync(sceneEvent);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "发送场景事件失败，移除订阅");
                        subscriptionsToRemove.Add(subscription);
                    }
                }));
            }

            await Task.WhenAll(tasks);

            // 移除失效的订阅
            foreach (var subscription in subscriptionsToRemove)
            {
                subscriptions.Remove(subscription);
            }
        }

        private bool ShouldSendEvent(EventSubscription subscription, SceneChangeEvent sceneEvent)
        {
            var filter = subscription.Filter;
            if (filter == null)
                return true;

            // 按实体ID过滤
            if (filter.EntityIds.Count > 0 && !filter.EntityIds.Contains(sceneEvent.EntityId))
            {
                return false;
            }

            // 按实体类型过滤
            if (filter.EntityTypes.Count > 0 && !filter.EntityTypes.Contains(sceneEvent.EntityId))
            {
                return false;
            }

            // 按用户过滤
            if (filter.UserIds.Count > 0 && !filter.UserIds.Contains(sceneEvent.UserId))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 获取会话的活跃订阅数量
        /// </summary>
        public int GetActiveSubscriptionCount(string sessionId)
        {
            return _sessionSubscriptions.GetValueOrDefault(sessionId, new List<EventSubscription>()).Count;
        }

        /// <summary>
        /// 获取所有活跃会话
        /// </summary>
        public IEnumerable<string> GetActiveSessionIds()
        {
            return _sessionSubscriptions.Keys;
        }

        /// <summary>
        /// 清理会话数据
        /// </summary>
        public void CleanupSession(string sessionId)
        {
            _sessionSubscriptions.TryRemove(sessionId, out _);
            _eventHistory.TryRemove(sessionId, out _);
            _logger.LogInformation($"清理会话事件数据: {sessionId}");
        }
    }

    /// <summary>
    /// 事件订阅信息
    /// </summary>
    internal class EventSubscription
    {
        public string SessionId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public IServerStreamWriter<SceneChangeEvent> Stream { get; set; } = null!;
        public HashSet<SceneChangeType> EventTypes { get; set; } = new();
        public EventFilter? Filter { get; set; }
    }
} 