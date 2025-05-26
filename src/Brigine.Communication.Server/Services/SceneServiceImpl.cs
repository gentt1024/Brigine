using Grpc.Core;
using Microsoft.Extensions.Logging;
using Brigine.Communication.Protos;
using Brigine.Core;
using CoreVector3 = System.Numerics.Vector3;
using CoreQuaternion = System.Numerics.Quaternion;
using ProtoVector3 = Brigine.Communication.Protos.Vector3;
using ProtoQuaternion = Brigine.Communication.Protos.Quaternion;
using ProtoTransform = Brigine.Communication.Protos.Transform;

namespace Brigine.Communication.Server.Services;

public class SceneServiceImpl : SceneService.SceneServiceBase
{
    private readonly ILogger<SceneServiceImpl> _logger;
    private readonly FrameworkServiceImpl _frameworkService;

    public SceneServiceImpl(ILogger<SceneServiceImpl> logger, FrameworkServiceImpl frameworkService)
    {
        _logger = logger;
        _frameworkService = frameworkService;
    }

    public override Task<AddEntityToSceneResponse> AddEntityToScene(AddEntityToSceneRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Adding entity {EntityName} to scene for framework: {FrameworkId}", 
                request.Entity.Name, request.FrameworkId);

            var framework = _frameworkService.GetFramework(request.FrameworkId);
            if (framework == null)
            {
                return Task.FromResult(new AddEntityToSceneResponse
                {
                    Success = false,
                    ErrorMessage = "Framework not found"
                });
            }

            var sceneService = framework.Services.GetService<ISceneService>();
            if (sceneService == null)
            {
                return Task.FromResult(new AddEntityToSceneResponse
                {
                    Success = false,
                    ErrorMessage = "Scene service not available"
                });
            }

            // 创建Core Entity
            var entity = new Entity(request.Entity.Name);
            
            // 设置变换
            if (request.Entity.Transform != null)
            {
                entity.Transform = ConvertProtoTransformToCoreTransform(request.Entity.Transform);
            }

            // 查找父实体
            Entity? parentEntity = null;
            if (!string.IsNullOrEmpty(request.ParentEntityId))
            {
                parentEntity = sceneService.GetEntity(request.ParentEntityId);
            }

            // 添加到场景
            sceneService.AddToScene(entity, parentEntity);

            return Task.FromResult(new AddEntityToSceneResponse
            {
                Success = true,
                EntityId = entity.Id
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

            var framework = _frameworkService.GetFramework(request.FrameworkId);
            if (framework == null)
            {
                return Task.FromResult(new RemoveEntityFromSceneResponse
                {
                    Success = false,
                    ErrorMessage = "Framework not found"
                });
            }

            var sceneService = framework.Services.GetService<ISceneService>();
            if (sceneService == null)
            {
                return Task.FromResult(new RemoveEntityFromSceneResponse
                {
                    Success = false,
                    ErrorMessage = "Scene service not available"
                });
            }

            sceneService.RemoveFromScene(request.EntityId);

            return Task.FromResult(new RemoveEntityFromSceneResponse
            {
                Success = true
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

            var framework = _frameworkService.GetFramework(request.FrameworkId);
            if (framework == null)
            {
                return Task.FromResult(new UpdateEntityTransformResponse
                {
                    Success = false,
                    ErrorMessage = "Framework not found"
                });
            }

            var sceneService = framework.Services.GetService<ISceneService>();
            if (sceneService == null)
            {
                return Task.FromResult(new UpdateEntityTransformResponse
                {
                    Success = false,
                    ErrorMessage = "Scene service not available"
                });
            }

            var entity = sceneService.GetEntity(request.EntityId);
            if (entity == null)
            {
                return Task.FromResult(new UpdateEntityTransformResponse
                {
                    Success = false,
                    ErrorMessage = "Entity not found"
                });
            }

            var coreTransform = ConvertProtoTransformToCoreTransform(request.Transform);
            sceneService.UpdateTransform(entity, coreTransform);

            return Task.FromResult(new UpdateEntityTransformResponse
            {
                Success = true
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

            var framework = _frameworkService.GetFramework(request.FrameworkId);
            if (framework == null)
            {
                return Task.FromResult(new GetSceneEntitiesResponse
                {
                    Success = false,
                    ErrorMessage = "Framework not found"
                });
            }

            var sceneService = framework.Services.GetService<ISceneService>();
            if (sceneService == null)
            {
                return Task.FromResult(new GetSceneEntitiesResponse
                {
                    Success = false,
                    ErrorMessage = "Scene service not available"
                });
            }

            var response = new GetSceneEntitiesResponse
            {
                Success = true
            };

            var entities = sceneService.GetEntities();
            foreach (var entity in entities)
            {
                // 如果指定了父实体ID，只返回该父实体的子实体
                if (!string.IsNullOrEmpty(request.ParentEntityId))
                {
                    if (entity.Parent?.Id != request.ParentEntityId)
                        continue;
                }
                // 如果没有指定父实体ID，只返回根实体（没有父实体的实体）
                else if (entity.Parent != null)
                {
                    continue;
                }

                var protoEntity = ConvertCoreEntityToProtoEntity(entity);
                response.Entities.Add(protoEntity);
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

            var framework = _frameworkService.GetFramework(request.FrameworkId);
            if (framework == null)
            {
                return Task.FromResult(new GetEntityInfoResponse
                {
                    Success = false,
                    ErrorMessage = "Framework not found"
                });
            }

            var sceneService = framework.Services.GetService<ISceneService>();
            if (sceneService == null)
            {
                return Task.FromResult(new GetEntityInfoResponse
                {
                    Success = false,
                    ErrorMessage = "Scene service not available"
                });
            }

            var entity = sceneService.GetEntity(request.EntityId);
            if (entity == null)
            {
                return Task.FromResult(new GetEntityInfoResponse
                {
                    Success = false,
                    ErrorMessage = "Entity not found"
                });
            }

            var protoEntity = ConvertCoreEntityToProtoEntity(entity);

            return Task.FromResult(new GetEntityInfoResponse
            {
                Success = true,
                Entity = protoEntity
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

    private Brigine.Core.Transform ConvertProtoTransformToCoreTransform(ProtoTransform protoTransform)
    {
        var position = new CoreVector3(protoTransform.Position.X, protoTransform.Position.Y, protoTransform.Position.Z);
        var rotation = new CoreQuaternion(protoTransform.Rotation.X, protoTransform.Rotation.Y, protoTransform.Rotation.Z, protoTransform.Rotation.W);
        
        return new Brigine.Core.Transform(position, rotation);
    }

    private ProtoTransform ConvertCoreTransformToProtoTransform(Brigine.Core.Transform coreTransform)
    {
        return new ProtoTransform
        {
            Position = new ProtoVector3
            {
                X = coreTransform.Position.X,
                Y = coreTransform.Position.Y,
                Z = coreTransform.Position.Z
            },
            Rotation = new ProtoQuaternion
            {
                X = coreTransform.Rotation.X,
                Y = coreTransform.Rotation.Y,
                Z = coreTransform.Rotation.Z,
                W = coreTransform.Rotation.W
            },
            Scale = new ProtoVector3
            {
                X = 1.0f, // Core Transform 没有 Scale，使用默认值
                Y = 1.0f,
                Z = 1.0f
            }
        };
    }

    private EntityInfo ConvertCoreEntityToProtoEntity(Entity coreEntity)
    {
        return new EntityInfo
        {
            EntityId = coreEntity.Id,
            Name = coreEntity.Name ?? string.Empty,
            Type = "Entity", // 可以根据组件类型确定更具体的类型
            Transform = ConvertCoreTransformToProtoTransform(coreEntity.Transform)
        };
    }
} 