using Grpc.Core;
using Microsoft.Extensions.Logging;
using Brigine.Communication.Protos;
using Brigine.Core;
using ProtoFrameworkStatus = Brigine.Communication.Protos.FrameworkStatus;

namespace Brigine.Communication.Server.Services;

public class FrameworkServiceImpl : FrameworkService.FrameworkServiceBase, IDisposable
{
    private readonly ILogger<FrameworkServiceImpl> _logger;
    private readonly IFrameworkManager _frameworkManager;
    private bool _disposed = false;

    public FrameworkServiceImpl(ILogger<FrameworkServiceImpl> logger)
    {
        _logger = logger;
        // 创建FrameworkManager实例，使用Core的ILogger
        var coreLogger = new CoreLoggerAdapter(logger);
        _frameworkManager = new FrameworkManager(coreLogger);
    }

    public override Task<StartFrameworkResponse> StartFramework(
        StartFrameworkRequest request, 
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Starting framework with providers: {Providers}", 
                string.Join(", ", request.FunctionProviders));

            // 使用FrameworkManager创建框架
            var frameworkId = _frameworkManager.CreateFramework(
                request.FunctionProviders, 
                request.Configuration.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            );

            // 启动框架
            var started = _frameworkManager.StartFramework(frameworkId);
            if (!started)
            {
                return Task.FromResult(new StartFrameworkResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to start framework"
                });
            }

            _logger.LogInformation("Framework started successfully: {FrameworkId}", frameworkId);

            return Task.FromResult(new StartFrameworkResponse
            {
                Success = true,
                FrameworkId = frameworkId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start framework");
            return Task.FromResult(new StartFrameworkResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            });
        }
    }

    public override Task<StopFrameworkResponse> StopFramework(
        StopFrameworkRequest request, 
        ServerCallContext context)
    {
        try
        {
            var stopped = _frameworkManager.StopFramework(request.FrameworkId);
            if (stopped)
            {
                _logger.LogInformation("Framework stopped: {FrameworkId}", request.FrameworkId);
                
                return Task.FromResult(new StopFrameworkResponse
                {
                    Success = true
                });
            }

            return Task.FromResult(new StopFrameworkResponse
            {
                Success = false,
                ErrorMessage = "Framework not found or failed to stop"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop framework: {FrameworkId}", request.FrameworkId);
            return Task.FromResult(new StopFrameworkResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            });
        }
    }

    public override Task<GetFrameworkStatusResponse> GetFrameworkStatus(
        GetFrameworkStatusRequest request, 
        ServerCallContext context)
    {
        try
        {
            var status = _frameworkManager.GetFrameworkStatus(request.FrameworkId);
            if (status != null)
            {
                var protoStatus = new ProtoFrameworkStatus
                {
                    IsRunning = status.IsRunning,
                    StartTime = ((DateTimeOffset)status.StartTime).ToUnixTimeSeconds()
                };

                // 添加已注册的服务
                foreach (var service in status.RegisteredServices)
                {
                    protoStatus.AvailableServices.Add(service);
                }

                // 添加配置信息
                foreach (var config in status.Configuration)
                {
                    protoStatus.Configuration[config.Key] = config.Value;
                }

                return Task.FromResult(new GetFrameworkStatusResponse
                {
                    Success = true,
                    Status = protoStatus
                });
            }

            return Task.FromResult(new GetFrameworkStatusResponse
            {
                Success = false,
                ErrorMessage = "Framework not found"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get framework status: {FrameworkId}", request.FrameworkId);
            return Task.FromResult(new GetFrameworkStatusResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            });
        }
    }

    public override Task<RegisterFunctionProviderResponse> RegisterFunctionProvider(
        RegisterFunctionProviderRequest request, 
        ServerCallContext context)
    {
        try
        {
            var registered = _frameworkManager.RegisterEngineProvider(
                request.FrameworkId, 
                request.ProviderType, 
                request.ProviderConfig.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            );

            if (registered)
            {
                _logger.LogInformation("Function provider registered: {ProviderType} for framework {FrameworkId}", 
                    request.ProviderType, request.FrameworkId);

                return Task.FromResult(new RegisterFunctionProviderResponse
                {
                    Success = true
                });
            }

            return Task.FromResult(new RegisterFunctionProviderResponse
            {
                Success = false,
                ErrorMessage = "Framework not found or failed to register provider"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register function provider: {ProviderType}", request.ProviderType);
            return Task.FromResult(new RegisterFunctionProviderResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            });
        }
    }

    public override Task<GetServiceResponse> GetService(
        GetServiceRequest request, 
        ServerCallContext context)
    {
        try
        {
            var framework = _frameworkManager.GetFramework(request.FrameworkId);
            if (framework != null)
            {
                // 根据服务类型名称查找服务
                var serviceType = GetServiceTypeByName(request.ServiceType);
                if (serviceType != null)
                {
                    var service = framework.Services.GetService(serviceType);
                    var isAvailable = service != null;

                    return Task.FromResult(new GetServiceResponse
                    {
                        Success = true,
                        ServiceId = isAvailable ? Guid.NewGuid().ToString() : string.Empty,
                        IsAvailable = isAvailable
                    });
                }
            }

            return Task.FromResult(new GetServiceResponse
            {
                Success = false,
                ErrorMessage = "Framework or service not found"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get service: {ServiceType}", request.ServiceType);
            return Task.FromResult(new GetServiceResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            });
        }
    }

    public override async Task FrameworkEvents(
        FrameworkEventsRequest request, 
        IServerStreamWriter<FrameworkEvent> responseStream, 
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Starting framework events stream for: {FrameworkId}", request.FrameworkId);

            // 检查框架是否存在
            var framework = _frameworkManager.GetFramework(request.FrameworkId);
            if (framework == null)
            {
                await responseStream.WriteAsync(new FrameworkEvent
                {
                    EventType = FrameworkEventType.ErrorOccurred,
                    FrameworkId = request.FrameworkId,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                });
                return;
            }

            // 发送初始事件
            await responseStream.WriteAsync(new FrameworkEvent
            {
                EventType = FrameworkEventType.FrameworkStarted,
                FrameworkId = request.FrameworkId,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            });

            // 保持连接直到客户端断开
            while (!context.CancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000, context.CancellationToken);
                
                // 检查框架状态变化
                var status = _frameworkManager.GetFrameworkStatus(request.FrameworkId);
                if (status == null)
                {
                    // 框架被移除
                    await responseStream.WriteAsync(new FrameworkEvent
                    {
                        EventType = FrameworkEventType.FrameworkStopped,
                        FrameworkId = request.FrameworkId,
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    });
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Framework events stream cancelled for: {FrameworkId}", request.FrameworkId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in framework events stream: {FrameworkId}", request.FrameworkId);
        }
    }

    // 获取Framework实例的公共方法，供其他服务使用
    public Framework? GetFramework(string frameworkId)
    {
        return _frameworkManager.GetFramework(frameworkId);
    }

    // 获取所有活跃的框架ID
    public IEnumerable<string> GetActiveFrameworks()
    {
        return _frameworkManager.GetActiveFrameworks();
    }

    private Type? GetServiceTypeByName(string serviceTypeName)
    {
        return serviceTypeName.ToLower() switch
        {
            "isceneservice" => typeof(ISceneService),
            "iassetserializer" => typeof(IAssetSerializer),
            "iupdateservice" => typeof(IUpdateService),
            "ilogger" => typeof(Brigine.Core.ILogger),
            _ => null
        };
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _frameworkManager?.Dispose();
            }
            _disposed = true;
        }
    }
}

// 适配器类，将Microsoft.Extensions.Logging.ILogger适配为Brigine.Core.ILogger
internal class CoreLoggerAdapter : Brigine.Core.ILogger
{
    private readonly ILogger<FrameworkServiceImpl> _logger;

    public CoreLoggerAdapter(ILogger<FrameworkServiceImpl> logger)
    {
        _logger = logger;
    }

    public void Info(string message) => _logger.LogInformation(message);
    public void Warn(string message) => _logger.LogWarning(message);
    public void Error(string message) => _logger.LogError(message);
    public void Debug(string message) => _logger.LogDebug(message);
} 