using Grpc.Net.Client;
using Brigine.Communication.Protos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;

namespace Brigine.Communication.Client
{
    /// <summary>
    /// Brigine通用gRPC客户端
    /// 支持所有.NET平台，包括Unity
    /// </summary>
    public class BrigineClient : IDisposable
    {
        private readonly GrpcChannel _channel;
        private readonly FrameworkService.FrameworkServiceClient _frameworkClient;
        private readonly AssetService.AssetServiceClient _assetClient;
        private readonly SceneService.SceneServiceClient _sceneClient;

        /// <summary>
        /// 创建Brigine客户端
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
            else
            {
                // 在Unity中，如果没有提供HttpHandler，会使用默认的UnityWebRequest
                // 这可能不支持HTTP/2，建议提供YetAnotherHttpHandler
                // 在其他.NET平台上，会使用默认的HttpClientHandler
            }

            _channel = GrpcChannel.ForAddress(serverAddress, channelOptions);
            _frameworkClient = new FrameworkService.FrameworkServiceClient(_channel);
            _assetClient = new AssetService.AssetServiceClient(_channel);
            _sceneClient = new SceneService.SceneServiceClient(_channel);
        }

        // Framework Service Methods
        public async Task<StartFrameworkResponse> StartFrameworkAsync(
            IEnumerable<string> functionProviders, 
            Dictionary<string, string>? configuration = null)
        {
            var request = new StartFrameworkRequest();
            request.FunctionProviders.AddRange(functionProviders);
        
            if (configuration != null)
            {
                foreach (var kvp in configuration)
                {
                    request.Configuration[kvp.Key] = kvp.Value;
                }
            }

            return await _frameworkClient.StartFrameworkAsync(request);
        }

        public async Task<StopFrameworkResponse> StopFrameworkAsync(string frameworkId)
        {
            var request = new StopFrameworkRequest { FrameworkId = frameworkId };
            return await _frameworkClient.StopFrameworkAsync(request);
        }

        public async Task<GetFrameworkStatusResponse> GetFrameworkStatusAsync(string frameworkId)
        {
            var request = new GetFrameworkStatusRequest { FrameworkId = frameworkId };
            return await _frameworkClient.GetFrameworkStatusAsync(request);
        }

        public async Task<RegisterFunctionProviderResponse> RegisterFunctionProviderAsync(
            string frameworkId, 
            string providerType, 
            Dictionary<string, string>? providerConfig = null)
        {
            var request = new RegisterFunctionProviderRequest
            {
                FrameworkId = frameworkId,
                ProviderType = providerType
            };

            if (providerConfig != null)
            {
                foreach (var kvp in providerConfig)
                {
                    request.ProviderConfig[kvp.Key] = kvp.Value;
                }
            }

            return await _frameworkClient.RegisterFunctionProviderAsync(request);
        }

        public async Task<GetServiceResponse> GetServiceAsync(string frameworkId, string serviceType)
        {
            var request = new GetServiceRequest
            {
                FrameworkId = frameworkId,
                ServiceType = serviceType
            };
            return await _frameworkClient.GetServiceAsync(request);
        }

        // Asset Service Methods
        public async Task<LoadAssetResponse> LoadAssetAsync(
            string frameworkId, 
            string assetPath, 
            bool async = false, 
            Dictionary<string, string>? loadOptions = null)
        {
            var request = new LoadAssetRequest
            {
                FrameworkId = frameworkId,
                AssetPath = assetPath,
                Async = async
            };

            if (loadOptions != null)
            {
                foreach (var kvp in loadOptions)
                {
                    request.LoadOptions[kvp.Key] = kvp.Value;
                }
            }

            return await _assetClient.LoadAssetAsync(request);
        }

        public async Task<UnloadAssetResponse> UnloadAssetAsync(string frameworkId, string assetId)
        {
            var request = new UnloadAssetRequest
            {
                FrameworkId = frameworkId,
                AssetId = assetId
            };
            return await _assetClient.UnloadAssetAsync(request);
        }

        public async Task<GetAssetInfoResponse> GetAssetInfoAsync(string frameworkId, string assetId)
        {
            var request = new GetAssetInfoRequest
            {
                FrameworkId = frameworkId,
                AssetId = assetId
            };
            return await _assetClient.GetAssetInfoAsync(request);
        }

        public async Task<ListAssetsResponse> ListAssetsAsync(
            string frameworkId, 
            string? pathFilter = null, 
            IEnumerable<string>? typeFilters = null)
        {
            var request = new ListAssetsRequest
            {
                FrameworkId = frameworkId,
                PathFilter = pathFilter ?? string.Empty
            };

            if (typeFilters != null)
            {
                request.TypeFilters.AddRange(typeFilters);
            }

            return await _assetClient.ListAssetsAsync(request);
        }

        // Scene Service Methods
        public async Task<AddEntityToSceneResponse> AddEntityToSceneAsync(
            string frameworkId, 
            EntityInfo entity, 
            string? parentEntityId = null)
        {
            var request = new AddEntityToSceneRequest
            {
                FrameworkId = frameworkId,
                Entity = entity,
                ParentEntityId = parentEntityId ?? string.Empty
            };
            return await _sceneClient.AddEntityToSceneAsync(request);
        }

        public async Task<RemoveEntityFromSceneResponse> RemoveEntityFromSceneAsync(
            string frameworkId, 
            string entityId)
        {
            var request = new RemoveEntityFromSceneRequest
            {
                FrameworkId = frameworkId,
                EntityId = entityId
            };
            return await _sceneClient.RemoveEntityFromSceneAsync(request);
        }

        public async Task<UpdateEntityTransformResponse> UpdateEntityTransformAsync(
            string frameworkId, 
            string entityId, 
            Transform transform)
        {
            var request = new UpdateEntityTransformRequest
            {
                FrameworkId = frameworkId,
                EntityId = entityId,
                Transform = transform
            };
            return await _sceneClient.UpdateEntityTransformAsync(request);
        }

        public async Task<GetSceneEntitiesResponse> GetSceneEntitiesAsync(
            string frameworkId, 
            string? parentEntityId = null)
        {
            var request = new GetSceneEntitiesRequest
            {
                FrameworkId = frameworkId,
                ParentEntityId = parentEntityId ?? string.Empty
            };
            return await _sceneClient.GetSceneEntitiesAsync(request);
        }

        public async Task<GetEntityInfoResponse> GetEntityInfoAsync(string frameworkId, string entityId)
        {
            var request = new GetEntityInfoRequest
            {
                FrameworkId = frameworkId,
                EntityId = entityId
            };
            return await _sceneClient.GetEntityInfoAsync(request);
        }

        // Helper methods for creating common objects
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

        public static EntityInfo CreateEntity(
            string name, 
            string type = "Entity", 
            Transform? transform = null)
        {
            var entity = new EntityInfo
            {
                Name = name,
                Type = type,
                Transform = transform ?? CreateTransform()
            };
            return entity;
        }

        public void Dispose()
        {
            _channel?.Dispose();
        }
    }
} 