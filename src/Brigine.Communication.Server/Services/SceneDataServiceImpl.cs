using Grpc.Core;
using Brigine.Communication.Protos;
using System.Collections.Concurrent;

namespace Brigine.Communication.Server.Services
{
    /// <summary>
    /// 场景数据服务实现 - 管理场景数据、实体和锁定机制
    /// </summary>
    public class SceneDataServiceImpl : SceneDataService.SceneDataServiceBase
    {
        private readonly ILogger<SceneDataServiceImpl> _logger;
        private readonly ConcurrentDictionary<string, SceneData> _sceneData = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, SceneEntity>> _sessionEntities = new();
        private readonly ConcurrentDictionary<string, EntityLock> _entityLocks = new();
        private long _versionCounter = 1;

        public SceneDataServiceImpl(ILogger<SceneDataServiceImpl> logger)
        {
            _logger = logger;
        }

        public override Task<GetSceneDataResponse> GetSceneData(GetSceneDataRequest request, ServerCallContext context)
        {
            try
            {
                var sessionId = request.SessionId;
                var sceneId = string.IsNullOrEmpty(request.SceneId) ? "default" : request.SceneId;

                if (!_sceneData.TryGetValue($"{sessionId}:{sceneId}", out var sceneData))
                {
                    // 创建默认场景数据
                    sceneData = new SceneData
                    {
                        SceneId = sceneId,
                        Name = $"Scene_{sceneId}",
                        Metadata = new SceneMetadata
                        {
                            CreatedBy = "system",
                            CreatedTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                            ModifiedBy = "system",
                            ModifiedTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                        },
                        Version = 1
                    };

                    _sceneData[$"{sessionId}:{sceneId}"] = sceneData;
                    _sessionEntities[$"{sessionId}:{sceneId}"] = new ConcurrentDictionary<string, SceneEntity>();
                }

                // 获取实体数据
                if (_sessionEntities.TryGetValue($"{sessionId}:{sceneId}", out var entities))
                {
                    sceneData.Entities.Clear();
                    sceneData.Entities.AddRange(entities.Values);
                }

                _logger.LogInformation($"获取场景数据: {sessionId}:{sceneId} ({sceneData.Entities.Count} 个实体)");

                return Task.FromResult(new GetSceneDataResponse
                {
                    Success = true,
                    SceneData = sceneData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取场景数据失败");
                return Task.FromResult(new GetSceneDataResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        public override Task<UpdateSceneDataResponse> UpdateSceneData(UpdateSceneDataRequest request, ServerCallContext context)
        {
            try
            {
                var sessionId = request.SessionId;
                var sceneId = request.SceneData.SceneId;
                var key = $"{sessionId}:{sceneId}";

                // 更新场景数据
                request.SceneData.Version = Interlocked.Increment(ref _versionCounter);
                request.SceneData.Metadata.ModifiedBy = request.UserId;
                request.SceneData.Metadata.ModifiedTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                _sceneData[key] = request.SceneData;

                // 更新实体数据
                var entities = _sessionEntities.GetOrAdd(key, _ => new ConcurrentDictionary<string, SceneEntity>());
                entities.Clear();

                foreach (var entity in request.SceneData.Entities)
                {
                    entities[entity.EntityId] = entity;
                }

                _logger.LogInformation($"更新场景数据: {key} (版本: {request.SceneData.Version}, 用户: {request.UserId})");

                return Task.FromResult(new UpdateSceneDataResponse
                {
                    Success = true,
                    Version = request.SceneData.Version
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新场景数据失败");
                return Task.FromResult(new UpdateSceneDataResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        public override Task<CreateEntityResponse> CreateEntity(CreateEntityRequest request, ServerCallContext context)
        {
            try
            {
                var sessionId = request.SessionId;
                var entity = request.Entity;

                // 生成实体ID（如果没有提供）
                if (string.IsNullOrEmpty(entity.EntityId))
                {
                    entity.EntityId = Guid.NewGuid().ToString();
                }

                // 设置元数据
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                entity.Metadata = entity.Metadata ?? new EntityMetadata();
                entity.Metadata.CreatedBy = request.UserId;
                entity.Metadata.CreatedTime = now;
                entity.Metadata.ModifiedBy = request.UserId;
                entity.Metadata.ModifiedTime = now;
                entity.Metadata.Version = Interlocked.Increment(ref _versionCounter);

                // 添加到会话实体集合
                var entities = _sessionEntities.GetOrAdd(sessionId, _ => new ConcurrentDictionary<string, SceneEntity>());
                entities[entity.EntityId] = entity;

                _logger.LogInformation($"创建实体: {entity.Name} [ID: {entity.EntityId[..8]}...] (用户: {request.UserId})");

                return Task.FromResult(new CreateEntityResponse
                {
                    Success = true,
                    EntityId = entity.EntityId,
                    Version = entity.Metadata.Version
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建实体失败");
                return Task.FromResult(new CreateEntityResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        public override Task<UpdateEntityResponse> UpdateEntity(UpdateEntityRequest request, ServerCallContext context)
        {
            try
            {
                var sessionId = request.SessionId;
                var entity = request.Entity;

                if (!_sessionEntities.TryGetValue(sessionId, out var entities) ||
                    !entities.ContainsKey(entity.EntityId))
                {
                    return Task.FromResult(new UpdateEntityResponse
                    {
                        Success = false,
                        ErrorMessage = "实体不存在"
                    });
                }

                // 更新元数据
                entity.Metadata = entity.Metadata ?? new EntityMetadata();
                entity.Metadata.ModifiedBy = request.UserId;
                entity.Metadata.ModifiedTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                entity.Metadata.Version = Interlocked.Increment(ref _versionCounter);

                entities[entity.EntityId] = entity;

                _logger.LogInformation($"更新实体: {entity.Name} [ID: {entity.EntityId[..8]}...] (用户: {request.UserId})");

                return Task.FromResult(new UpdateEntityResponse
                {
                    Success = true,
                    Version = entity.Metadata.Version
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新实体失败");
                return Task.FromResult(new UpdateEntityResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        public override Task<DeleteEntityResponse> DeleteEntity(DeleteEntityRequest request, ServerCallContext context)
        {
            try
            {
                var sessionId = request.SessionId;

                if (!_sessionEntities.TryGetValue(sessionId, out var entities) ||
                    !entities.TryRemove(request.EntityId, out var deletedEntity))
                {
                    return Task.FromResult(new DeleteEntityResponse
                    {
                        Success = false,
                        ErrorMessage = "实体不存在"
                    });
                }

                var version = Interlocked.Increment(ref _versionCounter);

                _logger.LogInformation($"删除实体: {deletedEntity.Name} [ID: {request.EntityId[..8]}...] (用户: {request.UserId})");

                return Task.FromResult(new DeleteEntityResponse
                {
                    Success = true,
                    Version = version
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除实体失败");
                return Task.FromResult(new DeleteEntityResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        public override Task<GetEntityResponse> GetEntity(GetEntityRequest request, ServerCallContext context)
        {
            try
            {
                var sessionId = request.SessionId;

                if (!_sessionEntities.TryGetValue(sessionId, out var entities) ||
                    !entities.TryGetValue(request.EntityId, out var entity))
                {
                    return Task.FromResult(new GetEntityResponse
                    {
                        Success = false,
                        ErrorMessage = "实体不存在"
                    });
                }

                return Task.FromResult(new GetEntityResponse
                {
                    Success = true,
                    Entity = entity
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取实体失败");
                return Task.FromResult(new GetEntityResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        public override Task<QueryEntitiesResponse> QueryEntities(QueryEntitiesRequest request, ServerCallContext context)
        {
            try
            {
                var sessionId = request.SessionId;
                var query = request.Query;

                if (!_sessionEntities.TryGetValue(sessionId, out var entities))
                {
                    return Task.FromResult(new QueryEntitiesResponse
                    {
                        Success = true,
                        Entities = { },
                        TotalCount = 0
                    });
                }

                var results = entities.Values.AsEnumerable();

                // 按ID过滤
                if (query.EntityIds.Count > 0)
                {
                    var idSet = query.EntityIds.ToHashSet();
                    results = results.Where(e => idSet.Contains(e.EntityId));
                }

                // 按类型过滤
                if (query.Types_.Count > 0)
                {
                    var typeSet = query.Types_.ToHashSet();
                    results = results.Where(e => typeSet.Contains(e.Type));
                }

                // 按父实体过滤
                if (!string.IsNullOrEmpty(query.ParentId))
                {
                    results = results.Where(e => e.ParentId == query.ParentId);
                }

                // 按标签过滤
                if (query.Tags.Count > 0)
                {
                    var tagSet = query.Tags.ToHashSet();
                    results = results.Where(e => e.Metadata?.Tags?.Any(tag => tagSet.Contains(tag)) == true);
                }

                var totalCount = results.Count();

                // 分页
                if (query.Offset > 0)
                {
                    results = results.Skip(query.Offset);
                }

                if (query.Limit > 0)
                {
                    results = results.Take(query.Limit);
                }

                return Task.FromResult(new QueryEntitiesResponse
                {
                    Success = true,
                    Entities = { results },
                    TotalCount = totalCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询实体失败");
                return Task.FromResult(new QueryEntitiesResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        public override async Task<BatchUpdateResponse> BatchUpdate(BatchUpdateRequest request, ServerCallContext context)
        {
            try
            {
                var results = new List<OperationResult>();
                var version = Interlocked.Increment(ref _versionCounter);

                foreach (var operation in request.Operations)
                {
                    try
                    {
                        switch (operation.OperationType)
                        {
                            case OperationType.Create:
                                var createResult = await CreateEntity(new CreateEntityRequest
                                {
                                    SessionId = request.SessionId,
                                    UserId = request.UserId,
                                    Entity = operation.Entity
                                }, context);

                                results.Add(new OperationResult
                                {
                                    Success = createResult.Success,
                                    ErrorMessage = createResult.ErrorMessage,
                                    EntityId = createResult.EntityId
                                });
                                break;

                            case OperationType.Update:
                                var updateResult = await UpdateEntity(new UpdateEntityRequest
                                {
                                    SessionId = request.SessionId,
                                    UserId = request.UserId,
                                    Entity = operation.Entity
                                }, context);

                                results.Add(new OperationResult
                                {
                                    Success = updateResult.Success,
                                    ErrorMessage = updateResult.ErrorMessage,
                                    EntityId = operation.Entity.EntityId
                                });
                                break;

                            case OperationType.Delete:
                                var deleteResult = await DeleteEntity(new DeleteEntityRequest
                                {
                                    SessionId = request.SessionId,
                                    UserId = request.UserId,
                                    EntityId = operation.Entity.EntityId
                                }, context);

                                results.Add(new OperationResult
                                {
                                    Success = deleteResult.Success,
                                    ErrorMessage = deleteResult.ErrorMessage,
                                    EntityId = operation.Entity.EntityId
                                });
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        results.Add(new OperationResult
                        {
                            Success = false,
                            ErrorMessage = ex.Message,
                            EntityId = operation.Entity?.EntityId ?? "unknown"
                        });
                    }
                }

                _logger.LogInformation($"批量操作完成: {results.Count} 个操作 (用户: {request.UserId})");

                return new BatchUpdateResponse
                {
                    Success = true,
                    Results = { results },
                    Version = version
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量更新失败");
                return new BatchUpdateResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public override Task<LockEntityResponse> LockEntity(LockEntityRequest request, ServerCallContext context)
        {
            try
            {
                var lockKey = request.EntityId;
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                // 检查是否已被锁定
                if (_entityLocks.TryGetValue(lockKey, out var existingLock))
                {
                    if (existingLock.UserId != request.UserId && existingLock.LockType == LockType.Exclusive)
                    {
                        return Task.FromResult(new LockEntityResponse
                        {
                            Success = false,
                            ErrorMessage = $"实体已被用户 {existingLock.UserId} 锁定"
                        });
                    }
                }

                var lockInfo = new EntityLock
                {
                    EntityId = request.EntityId,
                    UserId = request.UserId,
                    LockType = request.LockType,
                    AcquiredTime = now,
                    ExpiresTime = 0 // 永不过期，需要手动解锁
                };

                _entityLocks[lockKey] = lockInfo;

                _logger.LogInformation($"锁定实体: {request.EntityId} (用户: {request.UserId}, 类型: {request.LockType})");

                return Task.FromResult(new LockEntityResponse
                {
                    Success = true,
                    LockInfo = lockInfo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "锁定实体失败");
                return Task.FromResult(new LockEntityResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        public override Task<UnlockEntityResponse> UnlockEntity(UnlockEntityRequest request, ServerCallContext context)
        {
            try
            {
                var lockKey = request.EntityId;

                if (_entityLocks.TryGetValue(lockKey, out var lockInfo))
                {
                    if (lockInfo.UserId != request.UserId)
                    {
                        return Task.FromResult(new UnlockEntityResponse
                        {
                            Success = false,
                            ErrorMessage = "只有锁定者可以解锁实体"
                        });
                    }

                    _entityLocks.TryRemove(lockKey, out _);
                    _logger.LogInformation($"解锁实体: {request.EntityId} (用户: {request.UserId})");
                }

                return Task.FromResult(new UnlockEntityResponse
                {
                    Success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解锁实体失败");
                return Task.FromResult(new UnlockEntityResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        public override Task<GetEntityLocksResponse> GetEntityLocks(GetEntityLocksRequest request, ServerCallContext context)
        {
            try
            {
                var locks = new List<EntityLock>();

                if (request.EntityIds.Count == 0)
                {
                    // 返回所有锁定
                    locks.AddRange(_entityLocks.Values);
                }
                else
                {
                    // 返回指定实体的锁定
                    foreach (var entityId in request.EntityIds)
                    {
                        if (_entityLocks.TryGetValue(entityId, out var lockInfo))
                        {
                            locks.Add(lockInfo);
                        }
                    }
                }

                return Task.FromResult(new GetEntityLocksResponse
                {
                    Success = true,
                    Locks = { locks }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取实体锁定信息失败");
                return Task.FromResult(new GetEntityLocksResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }
    }
} 