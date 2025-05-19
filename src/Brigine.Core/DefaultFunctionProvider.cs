using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Brigine.Core
{
    internal class DefaultFunctionProvider : IFunctionProvider
    {
        private readonly Dictionary<Type, object> _serviceCache = new();

        public T GetService<T>() where T : class
        {
            var type = typeof(T);
            if (_serviceCache.TryGetValue(type, out var cachedService))
            {
                return (T)cachedService;
            }

            T service = null;
            if (type == typeof(IAssetSerializer))
            {
                service = new UsdNetAssetSerializer() as T;
            }
            else if (type == typeof(ISceneService))
            {
                service = new DefaultSceneService() as T;
            }
            else if (type == typeof(IUpdateService))
            {
                service = new DefaultUpdateService() as T;
            }
            else if (type == typeof(ILogger))
            {
                service = new DefaultLogger() as T;
            }

            if (service != null)
            {
                _serviceCache[type] = service;
            }

            return service;
        }

        private class DefaultSceneService : ISceneService
        {
            private readonly Dictionary<Entity, Transform> _sceneData = new();

            public IEnumerable<Entity> GetEntities() => _sceneData.Keys;

            public void AddToScene(Entity entity, Entity parent)
            {
                _sceneData[entity] = entity.Transform;
            }

            public void UpdateTransform(Entity entity, Transform transform)
            {
                _sceneData[entity] = transform;
            }
        }

        private class DefaultUpdateService : IUpdateService
        {
            private Action<float> _updateCallback;

            public DefaultUpdateService()
            {
                Task.Run(MainLoop);
            }

            public void RegisterUpdate(Action<float> updateCallback)
            {
                this._updateCallback = updateCallback;
            }

            private async void MainLoop()
            {
                while (true)
                {
                    _updateCallback?.Invoke(0.016f); // 模拟 60 FPS
                    await Task.Delay(16);
                }
            }
        }
        
        private class DefaultLogger : ILogger
        {
            public void Info(string message) => System.Console.WriteLine($"[INFO] {message}");
            public void Warn(string message) => System.Console.WriteLine($"[WARN] {message}");
            public void Error(string message) => System.Console.WriteLine($"[ERROR] {message}");
            public void Debug(string message) => System.Console.WriteLine($"[DEBUG] {message}");
        }
    }
}