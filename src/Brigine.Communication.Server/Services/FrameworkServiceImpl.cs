using Grpc.Core;
using Microsoft.Extensions.Logging;
using Brigine.Communication.Protos;
using Brigine.Core;
using ProtoFrameworkStatus = Brigine.Communication.Protos.FrameworkStatus;

namespace Brigine.Communication.Server.Services;

public class FrameworkServiceImpl : FrameworkService.FrameworkServiceBase
{
    private readonly ILogger<FrameworkServiceImpl> _logger;
    private readonly Dictionary<string, Framework> _frameworks = new();
    private readonly object _lock = new();

    public FrameworkServiceImpl(ILogger<FrameworkServiceImpl> logger)
    {
        _logger = logger;
    }

    public override Task<StartFrameworkResponse> StartFramework(
        StartFrameworkRequest request, 
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Starting framework with providers: {Providers}", 
                string.Join(", ", request.FunctionProviders));

            var frameworkId = Guid.NewGuid().ToString();
            var serviceRegistry = new ServiceRegistry();
            
            // 注册功能提供者
            foreach (var providerType in request.FunctionProviders)
            {
                _logger.LogDebug("Registering function provider: {ProviderType}", providerType);
                // 这里可以根据providerType动态注册不同的服务
            }

            var framework = new Framework(serviceRegistry);
            framework.Start();

            lock (_lock)
            {
                _frameworks[frameworkId] = framework;
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
            lock (_lock)
            {
                if (_frameworks.TryGetValue(request.FrameworkId, out var framework))
                {
                    framework.Stop();
                    _frameworks.Remove(request.FrameworkId);
                    
                    _logger.LogInformation("Framework stopped: {FrameworkId}", request.FrameworkId);
                    
                    return Task.FromResult(new StopFrameworkResponse
                    {
                        Success = true
                    });
                }
            }

            return Task.FromResult(new StopFrameworkResponse
            {
                Success = false,
                ErrorMessage = "Framework not found"
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
            lock (_lock)
            {
                if (_frameworks.TryGetValue(request.FrameworkId, out var framework))
                {
                    var status = new ProtoFrameworkStatus
                    {
                        IsRunning = framework.IsRunning,
                        StartTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() // 简化实现
                    };

                    // 获取可用服务 - 简化实现
                    status.AvailableServices.Add("AssetService");
                    status.AvailableServices.Add("SceneService");

                    return Task.FromResult(new GetFrameworkStatusResponse
                    {
                        Success = true,
                        Status = status
                    });
                }
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
            lock (_lock)
            {
                if (_frameworks.TryGetValue(request.FrameworkId, out var framework))
                {
                    _logger.LogInformation("Registering function provider {ProviderType} for framework {FrameworkId}", 
                        request.ProviderType, request.FrameworkId);

                    // 这里可以动态注册功能提供者
                    // 具体实现取决于你的功能提供者架构

                    return Task.FromResult(new RegisterFunctionProviderResponse
                    {
                        Success = true
                    });
                }
            }

            return Task.FromResult(new RegisterFunctionProviderResponse
            {
                Success = false,
                ErrorMessage = "Framework not found"
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
            lock (_lock)
            {
                if (_frameworks.TryGetValue(request.FrameworkId, out var framework))
                {
                    // 简化实现：假设服务总是可用的
                    return Task.FromResult(new GetServiceResponse
                    {
                        Success = true,
                        ServiceId = Guid.NewGuid().ToString(),
                        IsAvailable = true
                    });
                }
            }

            return Task.FromResult(new GetServiceResponse
            {
                Success = false,
                ErrorMessage = "Framework not found"
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
        _logger.LogInformation("Starting framework events stream for: {FrameworkId}", request.FrameworkId);

        try
        {
            // 这里可以实现事件流
            // 简化实现：发送一个测试事件
            var testEvent = new FrameworkEvent
            {
                EventType = FrameworkEventType.FrameworkStarted,
                FrameworkId = request.FrameworkId,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            await responseStream.WriteAsync(testEvent);

            // 保持连接直到客户端断开
            while (!context.CancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000, context.CancellationToken);
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
} 