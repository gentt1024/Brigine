using Grpc.Core;
using Microsoft.Extensions.Logging;
using Brigine.Communication.Protos;

namespace Brigine.Communication.Server.Services;

public class SceneServiceImpl : SceneService.SceneServiceBase
{
    private readonly ILogger<SceneServiceImpl> _logger;
    private readonly Dictionary<string, List<EntityInfo>> _sceneEntities = new();
    private readonly object _lock = new();

    public SceneServiceImpl(ILogger<SceneServiceImpl> logger)
    {
        _logger = logger;
    }

    public override Task<AddEntityToSceneResponse> AddEntityToScene(AddEntityToSceneRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Adding entity {EntityName} to scene for framework: {FrameworkId}", 
                request.Entity.Name, request.FrameworkId);

            var entityId = Guid.NewGuid().ToString();
            var entity = new EntityInfo
            {
                EntityId = entityId,
                Name = request.Entity.Name,
                Type = request.Entity.Type,
                Transform = request.Entity.Transform ?? new Transform
                {
                    Position = new Vector3 { X = 0, Y = 0, Z = 0 },
                    Rotation = new Quaternion { X = 0, Y = 0, Z = 0, W = 1 },
                    Scale = new Vector3 { X = 1, Y = 1, Z = 1 }
                }
            };

            lock (_lock)
            {
                if (!_sceneEntities.ContainsKey(request.FrameworkId))
                {
                    _sceneEntities[request.FrameworkId] = new List<EntityInfo>();
                }
                _sceneEntities[request.FrameworkId].Add(entity);
            }

            return Task.FromResult(new AddEntityToSceneResponse
            {
                Success = true,
                EntityId = entityId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add entity to scene for framework: {FrameworkId}", request.FrameworkId);
            return Task.FromResult(new AddEntityToSceneResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            });
        }
    }

    public override Task<RemoveEntityFromSceneResponse> RemoveEntityFromScene(RemoveEntityFromSceneRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Removing entity {EntityId} from scene for framework: {FrameworkId}", 
                request.EntityId, request.FrameworkId);

            lock (_lock)
            {
                if (_sceneEntities.TryGetValue(request.FrameworkId, out var entities))
                {
                    var entity = entities.FirstOrDefault(e => e.EntityId == request.EntityId);
                    if (entity != null)
                    {
                        entities.Remove(entity);
                        return Task.FromResult(new RemoveEntityFromSceneResponse
                        {
                            Success = true
                        });
                    }
                }
            }

            return Task.FromResult(new RemoveEntityFromSceneResponse
            {
                Success = false,
                ErrorMessage = "Entity not found"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove entity from scene: {EntityId}", request.EntityId);
            return Task.FromResult(new RemoveEntityFromSceneResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            });
        }
    }

    public override Task<UpdateEntityTransformResponse> UpdateEntityTransform(UpdateEntityTransformRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Updating transform for entity {EntityId} in framework: {FrameworkId}", 
                request.EntityId, request.FrameworkId);

            lock (_lock)
            {
                if (_sceneEntities.TryGetValue(request.FrameworkId, out var entities))
                {
                    var entity = entities.FirstOrDefault(e => e.EntityId == request.EntityId);
                    if (entity != null)
                    {
                        entity.Transform = request.Transform;
                        return Task.FromResult(new UpdateEntityTransformResponse
                        {
                            Success = true
                        });
                    }
                }
            }

            return Task.FromResult(new UpdateEntityTransformResponse
            {
                Success = false,
                ErrorMessage = "Entity not found"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update entity transform: {EntityId}", request.EntityId);
            return Task.FromResult(new UpdateEntityTransformResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            });
        }
    }

    public override Task<GetSceneEntitiesResponse> GetSceneEntities(GetSceneEntitiesRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Getting scene entities for framework: {FrameworkId}", request.FrameworkId);

            var response = new GetSceneEntitiesResponse
            {
                Success = true
            };

            lock (_lock)
            {
                if (_sceneEntities.TryGetValue(request.FrameworkId, out var entities))
                {
                    response.Entities.AddRange(entities);
                }
            }

            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get scene entities for framework: {FrameworkId}", request.FrameworkId);
            return Task.FromResult(new GetSceneEntitiesResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            });
        }
    }

    public override Task<GetEntityInfoResponse> GetEntityInfo(GetEntityInfoRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Getting entity info for {EntityId} in framework: {FrameworkId}", 
                request.EntityId, request.FrameworkId);

            lock (_lock)
            {
                if (_sceneEntities.TryGetValue(request.FrameworkId, out var entities))
                {
                    var entity = entities.FirstOrDefault(e => e.EntityId == request.EntityId);
                    if (entity != null)
                    {
                        return Task.FromResult(new GetEntityInfoResponse
                        {
                            Success = true,
                            Entity = entity
                        });
                    }
                }
            }

            return Task.FromResult(new GetEntityInfoResponse
            {
                Success = false,
                ErrorMessage = "Entity not found"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get entity info: {EntityId}", request.EntityId);
            return Task.FromResult(new GetEntityInfoResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            });
        }
    }
} 