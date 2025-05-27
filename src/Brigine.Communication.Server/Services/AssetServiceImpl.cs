using Grpc.Core;
using Microsoft.Extensions.Logging;
using Brigine.Communication.Protos;
using Brigine.Core;

namespace Brigine.Communication.Server.Services;

public class AssetServiceImpl : AssetService.AssetServiceBase
{
    private readonly ILogger<AssetServiceImpl> _logger;
    private readonly FrameworkServiceImpl _frameworkService;
    private readonly Dictionary<string, Dictionary<string, AssetInfo>> _assetCache = new();
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

            // 检查文件是否存在
            if (!File.Exists(request.AssetPath))
            {
                return Task.FromResult(new LoadAssetResponse
                {
                    Success = false,
                    ErrorMessage = $"Asset file not found: {request.AssetPath}"
                });
            }

            // 使用Framework的AssetManager真正加载资产
            framework.LoadAsset(request.AssetPath);

            var assetId = Guid.NewGuid().ToString();
            var fileInfo = new FileInfo(request.AssetPath);
            
            var assetInfo = new AssetInfo
            {
                AssetId = assetId,
                Path = request.AssetPath,
                Name = Path.GetFileNameWithoutExtension(request.AssetPath),
                Type = GetAssetTypeFromPath(request.AssetPath),
                Size = fileInfo.Length,
                LastModified = ((DateTimeOffset)fileInfo.LastWriteTime).ToUnixTimeSeconds(),
                IsLoaded = true
            };

            // 缓存资产信息
            lock (_lock)
            {
                if (!_assetCache.ContainsKey(request.FrameworkId))
                {
                    _assetCache[request.FrameworkId] = new Dictionary<string, AssetInfo>();
                }
                _assetCache[request.FrameworkId][assetId] = assetInfo;
            }

            _logger.LogInformation("Asset loaded successfully: {AssetPath} with ID: {AssetId}", 
                request.AssetPath, assetId);

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

            // 从缓存中获取资产信息，然后从Framework卸载
            AssetInfo assetInfo = null;
            lock (_lock)
            {
                if (_assetCache.TryGetValue(request.FrameworkId, out var frameworkAssets))
                {
                    if (frameworkAssets.TryGetValue(request.AssetId, out assetInfo))
                    {
                        frameworkAssets.Remove(request.AssetId);
                    }
                }
            }

            if (assetInfo == null)
            {
                return Task.FromResult(new UnloadAssetResponse
                {
                    Success = false,
                    ErrorMessage = "Asset not found in cache"
                });
            }

            // 注意：Core.AssetManager目前没有卸载方法，这里记录日志
            _logger.LogInformation("Asset unloaded from cache: {AssetPath}", assetInfo.Path);

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
                if (_assetCache.TryGetValue(request.FrameworkId, out var frameworkAssets) &&
                    frameworkAssets.TryGetValue(request.AssetId, out var assetInfo))
                {
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
                if (_assetCache.TryGetValue(request.FrameworkId, out var frameworkAssets))
                {
                    foreach (var kvp in frameworkAssets)
                    {
                        var assetInfo = kvp.Value;

                        // 应用过滤器
                        if (!string.IsNullOrEmpty(request.PathFilter) && 
                            !assetInfo.Path.Contains(request.PathFilter, StringComparison.OrdinalIgnoreCase))
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

            _logger.LogInformation("Listed {Count} assets for framework: {FrameworkId}", 
                response.Assets.Count, request.FrameworkId);

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
        var extension = Path.GetExtension(path).ToLower();
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