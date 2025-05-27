using Grpc.Net.Client;
using Brigine.Communication.Protos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Threading;

namespace Brigine.Communication.Client
{
    /// <summary>
    /// Brigine数据客户端 - 基于"数据即服务"架构
    /// 专注于协作会话管理、场景数据同步和实时事件流
    /// </summary>
    public class BrigineClient : IDisposable
    {
        private readonly GrpcChannel _channel;
        private readonly SessionService.SessionServiceClient _sessionClient;
        private readonly SceneDataService.SceneDataServiceClient _sceneClient;
        private readonly EventStreamService.EventStreamServiceClient _eventClient;

        /// <summary>
        /// 创建Brigine数据客户端
        /// </summary>
        /// <param name="serverAddress">服务器地址</param>
        /// <param name="httpHandler">HTTP处理器，Unity中推荐使用YetAnotherHttpHandler</param>
        public BrigineClient(string serverAddress = "http://localhost:50051", HttpMessageHandler? httpHandler = null)
        {
            var channelOptions = new GrpcChannelOptions();
        
            if (httpHandler != null)
            {
                channelOptions.HttpHandler = httpHandler;
                channelOptions.DisposeHttpClient = true;
            }

            _channel = GrpcChannel.ForAddress(serverAddress, channelOptions);
            _sessionClient = new SessionService.SessionServiceClient(_channel);
            _sceneClient = new SceneDataService.SceneDataServiceClient(_channel);
            _eventClient = new EventStreamService.EventStreamServiceClient(_channel);
        }

        #region Session Management

        /// <summary>
        /// 创建协作会话
        /// </summary>
        public async Task<CreateSessionResponse> CreateSessionAsync(
            string projectName, 
            string creatorId, 
            Dictionary<string, string>? metadata = null)
        {
            var request = new CreateSessionRequest
            {
                ProjectName = projectName,
                CreatorId = creatorId
            };

            if (metadata != null)
            {
                foreach (var kvp in metadata)
                {
                    request.Metadata[kvp.Key] = kvp.Value;
                }
            }

            return await _sessionClient.CreateSessionAsync(request);
        }

        /// <summary>
        /// 加入协作会话
        /// </summary>
        public async Task<JoinSessionResponse> JoinSessionAsync(
            string sessionId, 
            string userId, 
            string clientType = "Unknown",
            Dictionary<string, string>? clientMetadata = null)
        {
            var request = new JoinSessionRequest
            {
                SessionId = sessionId,
                UserId = userId,
                ClientType = clientType
            };

            if (clientMetadata != null)
            {
                foreach (var kvp in clientMetadata)
                {
                    request.ClientMetadata[kvp.Key] = kvp.Value;
                }
            }

            return await _sessionClient.JoinSessionAsync(request);
        }

        /// <summary>
        /// 离开协作会话
        /// </summary>
        public async Task<LeaveSessionResponse> LeaveSessionAsync(string sessionId, string userId)
        {
            var request = new LeaveSessionRequest
            {
                SessionId = sessionId,
                UserId = userId
            };
            return await _sessionClient.LeaveSessionAsync(request);
        }

        /// <summary>
        /// 获取会话信息
        /// </summary>
        public async Task<GetSessionInfoResponse> GetSessionInfoAsync(string sessionId)
        {
            var request = new GetSessionInfoRequest { SessionId = sessionId };
            return await _sessionClient.GetSessionInfoAsync(request);
        }

        /// <summary>
        /// 监听会话事件流
        /// </summary>
        public async Task StartSessionEventsAsync(
            string sessionId, 
            string userId,
            Action<SessionEvent> onSessionEvent, 
            CancellationToken cancellationToken = default,
            IEnumerable<SessionEventType>? eventTypes = null)
        {
            var request = new SessionEventsRequest
            {
                SessionId = sessionId,
                UserId = userId
            };

            if (eventTypes != null)
            {
                request.EventTypes.AddRange(eventTypes);
            }

            using var call = _sessionClient.SessionEvents(request, cancellationToken: cancellationToken);
            
            while (await call.ResponseStream.MoveNext(cancellationToken))
            {
                onSessionEvent(call.ResponseStream.Current);
            }
        }

        #endregion

        #region Scene Data Management

        /// <summary>
        /// 获取场景数据
        /// </summary>
        public async Task<GetSceneDataResponse> GetSceneDataAsync(string sessionId, string? sceneId = null)
        {
            var request = new GetSceneDataRequest
            {
                SessionId = sessionId,
                SceneId = sceneId ?? string.Empty
            };
            return await _sceneClient.GetSceneDataAsync(request);
        }

        /// <summary>
        /// 更新场景数据
        /// </summary>
        public async Task<UpdateSceneDataResponse> UpdateSceneDataAsync(
            string sessionId, 
            string userId, 
            SceneData sceneData)
        {
            var request = new UpdateSceneDataRequest
            {
                SessionId = sessionId,
                UserId = userId,
                SceneData = sceneData
            };
            return await _sceneClient.UpdateSceneDataAsync(request);
        }

        /// <summary>
        /// 创建实体
        /// </summary>
        public async Task<CreateEntityResponse> CreateEntityAsync(
            string sessionId, 
            string userId, 
            SceneEntity entity)
        {
            var request = new CreateEntityRequest
            {
                SessionId = sessionId,
                UserId = userId,
                Entity = entity
            };
            return await _sceneClient.CreateEntityAsync(request);
        }

        /// <summary>
        /// 更新实体
        /// </summary>
        public async Task<UpdateEntityResponse> UpdateEntityAsync(
            string sessionId, 
            string userId, 
            SceneEntity entity)
        {
            var request = new UpdateEntityRequest
            {
                SessionId = sessionId,
                UserId = userId,
                Entity = entity
            };
            return await _sceneClient.UpdateEntityAsync(request);
        }

        /// <summary>
        /// 删除实体
        /// </summary>
        public async Task<DeleteEntityResponse> DeleteEntityAsync(
            string sessionId, 
            string userId, 
            string entityId)
        {
            var request = new DeleteEntityRequest
            {
                SessionId = sessionId,
                UserId = userId,
                EntityId = entityId
            };
            return await _sceneClient.DeleteEntityAsync(request);
        }

        /// <summary>
        /// 获取实体
        /// </summary>
        public async Task<GetEntityResponse> GetEntityAsync(string sessionId, string entityId)
        {
            var request = new GetEntityRequest
            {
                SessionId = sessionId,
                EntityId = entityId
            };
            return await _sceneClient.GetEntityAsync(request);
        }

        /// <summary>
        /// 查询实体
        /// </summary>
        public async Task<QueryEntitiesResponse> QueryEntitiesAsync(string sessionId, EntityQuery query)
        {
            var request = new QueryEntitiesRequest
            {
                SessionId = sessionId,
                Query = query
            };
            return await _sceneClient.QueryEntitiesAsync(request);
        }

        /// <summary>
        /// 批量更新实体
        /// </summary>
        public async Task<BatchUpdateResponse> BatchUpdateAsync(
            string sessionId, 
            string userId, 
            IEnumerable<EntityOperation> operations)
        {
            var request = new BatchUpdateRequest
            {
                SessionId = sessionId,
                UserId = userId
            };
            request.Operations.AddRange(operations);

            return await _sceneClient.BatchUpdateAsync(request);
        }

        /// <summary>
        /// 锁定实体
        /// </summary>
        public async Task<LockEntityResponse> LockEntityAsync(
            string sessionId, 
            string userId, 
            string entityId, 
            LockType lockType = LockType.Exclusive)
        {
            var request = new LockEntityRequest
            {
                SessionId = sessionId,
                UserId = userId,
                EntityId = entityId,
                LockType = lockType
            };
            return await _sceneClient.LockEntityAsync(request);
        }

        /// <summary>
        /// 解锁实体
        /// </summary>
        public async Task<UnlockEntityResponse> UnlockEntityAsync(
            string sessionId, 
            string userId, 
            string entityId)
        {
            var request = new UnlockEntityRequest
            {
                SessionId = sessionId,
                UserId = userId,
                EntityId = entityId
            };
            return await _sceneClient.UnlockEntityAsync(request);
        }

        #endregion

        #region Event Stream Management

        /// <summary>
        /// 订阅场景变更事件流
        /// </summary>
        public async Task StartSceneEventsAsync(
            string sessionId, 
            string userId,
            Action<SceneChangeEvent> onSceneEvent, 
            CancellationToken cancellationToken = default,
            IEnumerable<SceneChangeType>? eventTypes = null,
            EventFilter? filter = null)
        {
            var request = new SubscribeEventsRequest
            {
                SessionId = sessionId,
                UserId = userId,
                Filter = filter
            };

            if (eventTypes != null)
            {
                request.EventTypes.AddRange(eventTypes);
            }

            using var call = _eventClient.SubscribeEvents(request, cancellationToken: cancellationToken);
            
            while (await call.ResponseStream.MoveNext(cancellationToken))
            {
                onSceneEvent(call.ResponseStream.Current);
            }
        }

        /// <summary>
        /// 发布场景变更事件
        /// </summary>
        public async Task<PublishEventResponse> PublishEventAsync(
            string sessionId, 
            string userId, 
            SceneChangeEvent sceneEvent)
        {
            var request = new PublishEventRequest
            {
                SessionId = sessionId,
                UserId = userId,
                Event = sceneEvent
            };
            return await _eventClient.PublishEventAsync(request);
        }

        /// <summary>
        /// 获取事件历史
        /// </summary>
        public async Task<GetEventHistoryResponse> GetEventHistoryAsync(
            string sessionId,
            long startTime,
            long endTime,
            IEnumerable<SceneChangeType>? eventTypes = null,
            int limit = 100,
            int offset = 0)
        {
            var request = new GetEventHistoryRequest
            {
                SessionId = sessionId,
                StartTime = startTime,
                EndTime = endTime,
                Limit = limit,
                Offset = offset
            };

            if (eventTypes != null)
            {
                request.EventTypes.AddRange(eventTypes);
            }

            return await _eventClient.GetEventHistoryAsync(request);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 创建变换对象
        /// </summary>
        public static Transform CreateTransform(
            float posX = 0, float posY = 0, float posZ = 0,
            float rotX = 0, float rotY = 0, float rotZ = 0, float rotW = 1,
            float scaleX = 1, float scaleY = 1, float scaleZ = 1)
        {
            return new Transform
            {
                Position = new Vector3 { X = posX, Y = posY, Z = posZ },
                Rotation = new Quaternion { X = rotX, Y = rotY, Z = rotZ, W = rotW },
                Scale = new Vector3 { X = scaleX, Y = scaleY, Z = scaleZ }
            };
        }

        /// <summary>
        /// 创建场景实体
        /// </summary>
        public static SceneEntity CreateEntity(
            string name, 
            string type, 
            Transform? transform = null,
            string? parentId = null,
            Dictionary<string, PropertyValue>? properties = null)
        {
            var entity = new SceneEntity
            {
                EntityId = Guid.NewGuid().ToString(),
                Name = name,
                Type = type,
                Transform = transform ?? CreateTransform(),
                ParentId = parentId ?? string.Empty,
                Metadata = new EntityMetadata
                {
                    CreatedTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    ModifiedTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Version = 1
                }
            };

            if (properties != null)
            {
                foreach (var kvp in properties)
                {
                    entity.Properties[kvp.Key] = kvp.Value;
                }
            }

            return entity;
        }

        /// <summary>
        /// 创建实体查询
        /// </summary>
        public static EntityQuery CreateQuery(
            IEnumerable<string>? entityIds = null,
            IEnumerable<string>? types = null,
            string? parentId = null,
            IEnumerable<string>? tags = null,
            int limit = 100,
            int offset = 0)
        {
            var query = new EntityQuery
            {
                ParentId = parentId ?? string.Empty,
                Limit = limit,
                Offset = offset
            };

            if (entityIds != null)
                query.EntityIds.AddRange(entityIds);
            if (types != null)
                query.Types_.AddRange(types);
            if (tags != null)
                query.Tags.AddRange(tags);

            return query;
        }

        #endregion

        public void Dispose()
        {
            _channel?.Dispose();
        }
    }
} 