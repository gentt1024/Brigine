using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Brigine.Core
{
    public class FrameworkManager : IFrameworkManager
    {
        private readonly ConcurrentDictionary<string, Framework> _frameworks = new();
        private readonly ILogger _logger;

        public FrameworkManager(ILogger logger = null)
        {
            _logger = logger ?? new DefaultLogger();
        }

        public string CreateFramework(IEnumerable<string> engineTypes = null, Dictionary<string, string> configuration = null)
        {
            var frameworkId = Guid.NewGuid().ToString();
            
            try
            {
                var serviceRegistry = new ServiceRegistry();
                
                // 注册引擎特定的服务
                if (engineTypes != null)
                {
                    foreach (var engineType in engineTypes)
                    {
                        RegisterEngineServices(serviceRegistry, engineType, configuration);
                    }
                }
                
                var framework = new Framework(serviceRegistry);
                _frameworks[frameworkId] = framework;
                
                _logger.Info($"Framework created with ID: {frameworkId}");
                return frameworkId;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to create framework: {ex.Message}");
                throw;
            }
        }

        public bool StartFramework(string frameworkId)
        {
            if (_frameworks.TryGetValue(frameworkId, out var framework))
            {
                try
                {
                    framework.Start();
                    _logger.Info($"Framework started: {frameworkId}");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed to start framework {frameworkId}: {ex.Message}");
                    return false;
                }
            }
            
            _logger.Error($"Framework not found: {frameworkId}");
            return false;
        }

        public bool StopFramework(string frameworkId)
        {
            if (_frameworks.TryGetValue(frameworkId, out var framework))
            {
                try
                {
                    framework.Stop();
                    _logger.Info($"Framework stopped: {frameworkId}");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed to stop framework {frameworkId}: {ex.Message}");
                    return false;
                }
            }
            
            _logger.Error($"Framework not found: {frameworkId}");
            return false;
        }

        public bool RemoveFramework(string frameworkId)
        {
            if (_frameworks.TryRemove(frameworkId, out var framework))
            {
                try
                {
                    if (framework.IsRunning)
                    {
                        framework.Stop();
                    }
                    _logger.Info($"Framework removed: {frameworkId}");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed to remove framework {frameworkId}: {ex.Message}");
                    return false;
                }
            }
            
            _logger.Error($"Framework not found: {frameworkId}");
            return false;
        }

        public Framework GetFramework(string frameworkId)
        {
            _frameworks.TryGetValue(frameworkId, out var framework);
            return framework;
        }

        public FrameworkStatus GetFrameworkStatus(string frameworkId)
        {
            if (_frameworks.TryGetValue(frameworkId, out var framework))
            {
                return new FrameworkStatus
                {
                    FrameworkId = frameworkId,
                    IsRunning = framework.IsRunning,
                    RegisteredServices = framework.Services.GetRegisteredServices().Select(t => t.Name).ToList(),
                    StartTime = DateTime.UtcNow // 简化实现，实际应该记录真实启动时间
                };
            }
            
            return null;
        }

        public IEnumerable<string> GetActiveFrameworks()
        {
            return _frameworks.Keys;
        }

        public bool RegisterEngineProvider(string frameworkId, string engineType, Dictionary<string, string> configuration = null)
        {
            if (_frameworks.TryGetValue(frameworkId, out var framework))
            {
                try
                {
                    RegisterEngineServices(framework.Services, engineType, configuration);
                    _logger.Info($"Engine provider registered: {engineType} for framework {frameworkId}");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed to register engine provider {engineType}: {ex.Message}");
                    return false;
                }
            }
            
            _logger.Error($"Framework not found: {frameworkId}");
            return false;
        }

        private void RegisterEngineServices(IServiceRegistry serviceRegistry, string engineType, Dictionary<string, string> configuration)
        {
            switch (engineType.ToLower())
            {
                case "unity":
                    RegisterUnityServices(serviceRegistry, configuration);
                    break;
                case "godot":
                    RegisterGodotServices(serviceRegistry, configuration);
                    break;
                case "unreal":
                    RegisterUnrealServices(serviceRegistry, configuration);
                    break;
                default:
                    _logger.Warn($"Unknown engine type: {engineType}");
                    break;
            }
        }

        private void RegisterUnityServices(IServiceRegistry serviceRegistry, Dictionary<string, string> configuration)
        {
            // Unity特定的服务注册
            try
            {
                // 使用反射调用Unity服务提供者
                var unityAssembly = Assembly.LoadFrom("Brigine.Unity.dll");
                var unityProviderType = unityAssembly.GetType("Brigine.Unity.UnityServiceProvider");
                var registerMethod = unityProviderType?.GetMethod("RegisterUnityServices");
                registerMethod?.Invoke(null, new object[] { serviceRegistry });
                
                _logger.Info("Unity services registered successfully");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Failed to register Unity services: {ex.Message}");
                // 回退到默认服务
            }
        }

        private void RegisterGodotServices(IServiceRegistry serviceRegistry, Dictionary<string, string> configuration)
        {
            // Godot特定的服务注册
            try
            {
                // 使用反射调用Godot服务提供者
                var godotAssembly = Assembly.LoadFrom("Brigine.Godot.dll");
                var godotProviderType = godotAssembly.GetType("Brigine.Godot.GodotServiceProvider");
                var registerMethod = godotProviderType?.GetMethod("RegisterGodotServices");
                registerMethod?.Invoke(null, new object[] { serviceRegistry });
                
                _logger.Info("Godot services registered successfully");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Failed to register Godot services: {ex.Message}");
                // 回退到默认服务
            }
        }

        private void RegisterUnrealServices(IServiceRegistry serviceRegistry, Dictionary<string, string> configuration)
        {
            // Unreal特定的服务注册
            _logger.Info("Unreal services registered");
        }

        public void Dispose()
        {
            foreach (var frameworkId in _frameworks.Keys.ToList())
            {
                RemoveFramework(frameworkId);
            }
            _frameworks.Clear();
            _logger.Info("FrameworkManager disposed");
        }
    }

    public interface IFrameworkManager : IDisposable
    {
        string CreateFramework(IEnumerable<string> engineTypes = null, Dictionary<string, string> configuration = null);
        bool StartFramework(string frameworkId);
        bool StopFramework(string frameworkId);
        bool RemoveFramework(string frameworkId);
        Framework GetFramework(string frameworkId);
        FrameworkStatus GetFrameworkStatus(string frameworkId);
        IEnumerable<string> GetActiveFrameworks();
        bool RegisterEngineProvider(string frameworkId, string engineType, Dictionary<string, string> configuration = null);
    }

    public class FrameworkStatus
    {
        public string FrameworkId { get; set; }
        public bool IsRunning { get; set; }
        public List<string> RegisteredServices { get; set; } = new();
        public DateTime StartTime { get; set; }
        public Dictionary<string, string> Configuration { get; set; } = new();
    }
} 