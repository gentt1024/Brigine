using Grpc.Core;
using Microsoft.Extensions.Logging;
using Brigine.Communication.Protos;
using Brigine.Core;

namespace Brigine.Communication.Server.Services;

public class AssetServiceImpl : AssetService.AssetServiceBase
{
    private readonly ILogger<AssetServiceImpl> _logger;
    private readonly FrameworkServiceImpl _frameworkService;
    private readonly Dictionary<string, Dictionary<string, object>> _loadedAssets = new();
    private readonly object _lock = new();

    public AssetServiceImpl(ILogger<AssetServiceImpl> logger, FrameworkServiceImpl frameworkService)
    {
        _logger = logger;
        _frameworkService = frameworkService;
    }

    public override Task<LoadAssetResponse> LoadAsset(LoadAssetRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Loading asset: {AssetPath} for framework: {FrameworkId}", 
                request.AssetPath, request.FrameworkId);

            var framework = _frameworkService.GetFramework(request.FrameworkId);
            if (framework == null)
            {
                return Task.FromResult(new LoadAssetResponse
                {
                    Success = false,
                    ErrorMessage = "Framework not found"
                });
            }

            // 使用Framework的LoadAsset方法
            framework.LoadAsset(request.AssetPath);

            var assetId = Guid.NewGuid().ToString();
            
            // 记录已加载的资产
            lock (_lock)
            {
                if (!_loadedAssets.ContainsKey(request.FrameworkId))
                {
                    _loadedAssets[request.FrameworkId] = new Dictionary<string, object>();
                }
                _loadedAssets[request.FrameworkId][assetId] = new
                {
                    AssetId = assetId,
                    Path = request.AssetPath,
                    LoadTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    LoadOptions = request.LoadOptions
                };
            }

            var assetInfo = new AssetInfo
            {
                AssetId = assetId,
                Path = request.AssetPath,
                Name = System.IO.Path.GetFileNameWithoutExtension(request.AssetPath),
                Type = GetAssetTypeFromPath(request.AssetPath),
                Size = 0, // 实际实现中应该获取文件大小
                LastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                IsLoaded = true
            };

            return Task.FromResult(new LoadAssetResponse
            {
                Success = true,
                AssetId = assetId,
                AssetInfo = assetInfo
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

            var framework = _frameworkService.GetFramework(request.FrameworkId);
            if (framework == null)
            {
                return Task.FromResult(new UnloadAssetResponse
                {
                    Success = false,
                    ErrorMessage = "Framework not found"
                });
            }

            // 从已加载资产中移除
            lock (_lock)
            {
                if (_loadedAssets.TryGetValue(request.FrameworkId, out var frameworkAssets))
                {
                    frameworkAssets.Remove(request.AssetId);
                }
            }

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

            var framework = _frameworkService.GetFramework(request.FrameworkId);
            if (framework == null)
            {
                return Task.FromResult(new GetAssetInfoResponse
                {
                    Success = false,
                    ErrorMessage = "Framework not found"
                });
            }

            lock (_lock)
            {
                if (_loadedAssets.TryGetValue(request.FrameworkId, out var frameworkAssets) &&
                    frameworkAssets.TryGetValue(request.AssetId, out var assetData))
                {
                    var data = (dynamic)assetData;
                    var assetInfo = new AssetInfo
                    {
                        AssetId = data.AssetId,
                        Path = data.Path,
                        Name = System.IO.Path.GetFileNameWithoutExtension(data.Path),
                        Type = GetAssetTypeFromPath(data.Path),
                        Size = 0,
                        LastModified = data.LoadTime,
                        IsLoaded = true
                    };

                    return Task.FromResult(new GetAssetInfoResponse
                    {
                        Success = true,
                        AssetInfo = assetInfo
                    });
                }
            }

            return Task.FromResult(new GetAssetInfoResponse
            {
                Success = false,
                ErrorMessage = "Asset not found"
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

            var framework = _frameworkService.GetFramework(request.FrameworkId);
            if (framework == null)
            {
                return Task.FromResult(new ListAssetsResponse
                {
                    Success = false,
                    ErrorMessage = "Framework not found"
                });
            }

            var response = new ListAssetsResponse
            {
                Success = true
            };

            lock (_lock)
            {
                if (_loadedAssets.TryGetValue(request.FrameworkId, out var frameworkAssets))
                {
                    foreach (var kvp in frameworkAssets)
                    {
                        var data = (dynamic)kvp.Value;
                        var assetInfo = new AssetInfo
                        {
                            AssetId = data.AssetId,
                            Path = data.Path,
                            Name = System.IO.Path.GetFileNameWithoutExtension(data.Path),
                            Type = GetAssetTypeFromPath(data.Path),
                            Size = 0,
                            LastModified = data.LoadTime,
                            IsLoaded = true
                        };

                        // 应用过滤器
                        if (!string.IsNullOrEmpty(request.PathFilter) && 
                            !data.Path.Contains(request.PathFilter, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (request.TypeFilters.Count > 0 && 
                            !request.TypeFilters.Contains(assetInfo.Type.ToString()))
                        {
                            continue;
                        }

                        response.Assets.Add(assetInfo);
                    }
                }
            }

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

    private AssetType GetAssetTypeFromPath(string path)
    {
        var extension = System.IO.Path.GetExtension(path).ToLower();
        return extension switch
        {
            ".usd" or ".usda" or ".usdc" => AssetType.UsdScene,
            ".fbx" or ".obj" or ".dae" => AssetType.UsdMesh,
            ".png" or ".jpg" or ".jpeg" or ".tga" or ".bmp" => AssetType.Texture,
            ".wav" or ".mp3" or ".ogg" => AssetType.Audio,
            ".cs" or ".js" or ".py" => AssetType.Script,
            _ => AssetType.Unknown
        };
    }
} 