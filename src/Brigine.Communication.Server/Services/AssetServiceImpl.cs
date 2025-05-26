using Grpc.Core;
using Microsoft.Extensions.Logging;
using Brigine.Communication.Protos;

namespace Brigine.Communication.Server.Services;

public class AssetServiceImpl : AssetService.AssetServiceBase
{
    private readonly ILogger<AssetServiceImpl> _logger;

    public AssetServiceImpl(ILogger<AssetServiceImpl> logger)
    {
        _logger = logger;
    }

    public override Task<LoadAssetResponse> LoadAsset(LoadAssetRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Loading asset: {AssetPath} for framework: {FrameworkId}", 
                request.AssetPath, request.FrameworkId);

            // 简化实现：模拟资产加载
            var assetId = Guid.NewGuid().ToString();
            
            return Task.FromResult(new LoadAssetResponse
            {
                Success = true,
                AssetId = assetId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load asset: {AssetPath}", request.AssetPath);
            return Task.FromResult(new LoadAssetResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            });
        }
    }

    public override Task<UnloadAssetResponse> UnloadAsset(UnloadAssetRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Unloading asset: {AssetId} for framework: {FrameworkId}", 
                request.AssetId, request.FrameworkId);

            return Task.FromResult(new UnloadAssetResponse
            {
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unload asset: {AssetId}", request.AssetId);
            return Task.FromResult(new UnloadAssetResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            });
        }
    }

    public override Task<GetAssetInfoResponse> GetAssetInfo(GetAssetInfoRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Getting asset info: {AssetId} for framework: {FrameworkId}", 
                request.AssetId, request.FrameworkId);

            var assetInfo = new AssetInfo
            {
                AssetId = request.AssetId,
                Path = "mock/path/asset.usd",
                Name = "MockAsset",
                Type = AssetType.UsdScene,
                Size = 1024,
                LastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                IsLoaded = true
            };

            return Task.FromResult(new GetAssetInfoResponse
            {
                Success = true,
                AssetInfo = assetInfo
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get asset info: {AssetId}", request.AssetId);
            return Task.FromResult(new GetAssetInfoResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            });
        }
    }

    public override Task<ListAssetsResponse> ListAssets(ListAssetsRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Listing assets for framework: {FrameworkId}", request.FrameworkId);

            var response = new ListAssetsResponse
            {
                Success = true
            };

            // 模拟一些资产
            response.Assets.Add(new AssetInfo
            {
                AssetId = Guid.NewGuid().ToString(),
                Path = "scenes/test_scene.usd",
                Name = "TestScene",
                Type = AssetType.UsdScene,
                Size = 2048,
                LastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                IsLoaded = false
            });

            response.Assets.Add(new AssetInfo
            {
                AssetId = Guid.NewGuid().ToString(),
                Path = "models/test_model.fbx",
                Name = "TestModel",
                Type = AssetType.UsdMesh,
                Size = 4096,
                LastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                IsLoaded = false
            });

            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list assets for framework: {FrameworkId}", request.FrameworkId);
            return Task.FromResult(new ListAssetsResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            });
        }
    }
} 