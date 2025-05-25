using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Brigine.Core
{
    public class ServiceRegistry : IServiceRegistry
    {
        private readonly ConcurrentDictionary<Type, ServiceDescriptor> _services = new();
        private readonly ConcurrentDictionary<Type, object> _singletonInstances = new();
        private readonly object _lock = new object();

        public void RegisterSingleton<TInterface, TImplementation>()
            where TInterface : class
            where TImplementation : class, TInterface, new()
        {
            RegisterSingleton<TInterface>(() => new TImplementation());
        }

        public void RegisterSingleton<TInterface>(Func<TInterface> factory)
            where TInterface : class
        {
            _services[typeof(TInterface)] = new ServiceDescriptor
            {
                ServiceType = typeof(TInterface),
                Lifetime = ServiceLifetime.Singleton,
                Factory = () => factory()
            };
        }

        public void RegisterTransient<TInterface, TImplementation>()
            where TInterface : class
            where TImplementation : class, TInterface, new()
        {
            RegisterTransient<TInterface>(() => new TImplementation());
        }

        public void RegisterTransient<TInterface>(Func<TInterface> factory)
            where TInterface : class
        {
            _services[typeof(TInterface)] = new ServiceDescriptor
            {
                ServiceType = typeof(TInterface),
                Lifetime = ServiceLifetime.Transient,
                Factory = () => factory()
            };
        }

        public void RegisterInstance<TInterface>(TInterface instance)
            where TInterface : class
        {
            _singletonInstances[typeof(TInterface)] = instance;
            _services[typeof(TInterface)] = new ServiceDescriptor
            {
                ServiceType = typeof(TInterface),
                Lifetime = ServiceLifetime.Singleton,
                Factory = () => instance
            };
        }

        public T GetService<T>() where T : class
        {
            return (T)GetService(typeof(T));
        }

        public object GetService(Type serviceType)
        {
            if (!_services.TryGetValue(serviceType, out var descriptor))
            {
                return null;
            }

            if (descriptor.Lifetime == ServiceLifetime.Singleton)
            {
                if (_singletonInstances.TryGetValue(serviceType, out var instance))
                {
                    return instance;
                }

                lock (_lock)
                {
                    if (_singletonInstances.TryGetValue(serviceType, out instance))
                    {
                        return instance;
                    }

                    instance = descriptor.Factory();
                    _singletonInstances[serviceType] = instance;
                    return instance;
                }
            }

            return descriptor.Factory();
        }

        public bool IsRegistered<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }

        public bool IsRegistered(Type serviceType)
        {
            return _services.ContainsKey(serviceType);
        }

        public IEnumerable<Type> GetRegisteredServices()
        {
            return _services.Keys;
        }

        public void RegisterDefaultServices()
        {
            // 注册默认服务实现
            if (!IsRegistered<ILogger>())
                RegisterSingleton<ILogger, DefaultLogger>();
            
            if (!IsRegistered<IAssetSerializer>())
                RegisterSingleton<IAssetSerializer, UsdNetAssetSerializer>();
            
            if (!IsRegistered<ISceneService>())
                RegisterSingleton<ISceneService, DefaultSceneService>();
            
            if (!IsRegistered<IUpdateService>())
                RegisterSingleton<IUpdateService, DefaultUpdateService>();
        }
    }

    public interface IServiceRegistry
    {
        void RegisterSingleton<TInterface, TImplementation>()
            where TInterface : class
            where TImplementation : class, TInterface, new();
        
        void RegisterSingleton<TInterface>(Func<TInterface> factory)
            where TInterface : class;
        
        void RegisterTransient<TInterface, TImplementation>()
            where TInterface : class
            where TImplementation : class, TInterface, new();
        
        void RegisterTransient<TInterface>(Func<TInterface> factory)
            where TInterface : class;
        
        void RegisterInstance<TInterface>(TInterface instance)
            where TInterface : class;
        
        T GetService<T>() where T : class;
        object GetService(Type serviceType);
        bool IsRegistered<T>() where T : class;
        bool IsRegistered(Type serviceType);
        IEnumerable<Type> GetRegisteredServices();
        void RegisterDefaultServices();
    }

    public class ServiceDescriptor
    {
        public Type ServiceType { get; set; }
        public ServiceLifetime Lifetime { get; set; }
        public Func<object> Factory { get; set; }
    }

    public enum ServiceLifetime
    {
        Transient,
        Singleton
    }

    // 默认服务实现
    internal class DefaultLogger : ILogger
    {
        public void Info(string message) => Console.WriteLine($"[INFO] {DateTime.Now:HH:mm:ss} {message}");
        public void Warn(string message) => Console.WriteLine($"[WARN] {DateTime.Now:HH:mm:ss} {message}");
        public void Error(string message) => Console.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss} {message}");
        public void Debug(string message) => Console.WriteLine($"[DEBUG] {DateTime.Now:HH:mm:ss} {message}");
    }

    internal class DefaultSceneService : ISceneService
    {
        private readonly ConcurrentDictionary<string, Entity> _entities = new();
        private readonly ILogger _logger;

        public DefaultSceneService(IServiceRegistry registry)
        {
            _logger = registry.GetService<ILogger>();
        }

        public DefaultSceneService() : this(new ServiceRegistry()) { }

        public IEnumerable<Entity> GetEntities() => _entities.Values;

        public void AddToScene(Entity entity, Entity parent)
        {
            _entities[entity.Id] = entity;
            if (parent != null)
            {
                entity.Parent = parent;
                parent.Children.Add(entity);
            }
            _logger?.Info($"Added entity {entity.Name} to scene");
        }

        public void UpdateTransform(Entity entity, Transform transform)
        {
            if (_entities.ContainsKey(entity.Id))
            {
                entity.Transform = transform;
                _logger?.Debug($"Updated transform for entity {entity.Name}");
            }
        }

        public Entity GetEntity(string entityId)
        {
            _entities.TryGetValue(entityId, out var entity);
            return entity;
        }

        public void RemoveFromScene(string entityId)
        {
            if (_entities.TryRemove(entityId, out var entity))
            {
                if (entity.Parent != null)
                {
                    entity.Parent.Children.Remove(entity);
                }
                _logger?.Info($"Removed entity {entity.Name} from scene");
            }
        }
    }

    internal class DefaultUpdateService : IUpdateService
    {
        private readonly List<Action<float>> _updateCallbacks = new();
        private readonly ILogger _logger;
        private bool _isRunning = false;

        public DefaultUpdateService(IServiceRegistry registry)
        {
            _logger = registry.GetService<ILogger>();
        }

        public DefaultUpdateService() : this(new ServiceRegistry()) { }

        public void RegisterUpdate(Action<float> updateCallback)
        {
            _updateCallbacks.Add(updateCallback);
            if (!_isRunning)
            {
                _isRunning = true;
                _ = Task.Run(MainLoop);
                _logger?.Info("Update service started");
            }
        }

        private async Task MainLoop()
        {
            var lastTime = DateTime.UtcNow;
            while (_isRunning && _updateCallbacks.Count > 0)
            {
                var currentTime = DateTime.UtcNow;
                var deltaTime = (float)(currentTime - lastTime).TotalSeconds;
                lastTime = currentTime;

                foreach (var callback in _updateCallbacks)
                {
                    try
                    {
                        callback(deltaTime);
                    }
                    catch (Exception ex)
                    {
                        _logger?.Error($"Update callback error: {ex.Message}");
                    }
                }

                await Task.Delay(16); // ~60 FPS
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _logger?.Info("Update service stopped");
        }
    }
}