using System;
using System.Collections.Generic;

namespace Brigine.Core
{
    public class Framework
    {
        private readonly IServiceRegistry _serviceRegistry;
        private readonly AssetManager _assetManager;
        private bool _isRunning = false;

        public IServiceRegistry Services => _serviceRegistry;
        public ILogger Logger => Services.GetService<ILogger>();
        public bool IsRunning => _isRunning;

        public Framework(IServiceRegistry serviceRegistry = null)
        {
            _serviceRegistry = serviceRegistry ?? new ServiceRegistry();
            
            // 注册默认服务
            _serviceRegistry.RegisterDefaultServices();
            
            _assetManager = new AssetManager(_serviceRegistry);
            Logger.Info("Framework initialized");
        }

        public void Start()
        {
            if (_isRunning)
            {
                Logger.Warn("Framework is already running");
                return;
            }

            var updateService = _serviceRegistry.GetService<IUpdateService>();
            if (updateService == null)
            {
                Logger.Error("IUpdateService not found in ServiceRegistry");
                throw new InvalidOperationException("IUpdateService is required");
            }
            
            updateService.RegisterUpdate(Update);
            _isRunning = true;
            Logger.Info("Framework started");
        }

        public void Stop()
        {
            if (!_isRunning)
            {
                Logger.Warn("Framework is not running");
                return;
            }

            var updateService = _serviceRegistry.GetService<IUpdateService>();
            updateService?.Stop();
            
            _isRunning = false;
            Logger.Info("Framework stopped");
        }

        public void LoadAsset(string assetPath)
        {
            Logger.Info($"Loading scene from {assetPath}");
            var asset = _assetManager.LoadAsset(assetPath);
            if (asset == null)
            {
                Logger.Error($"Failed to load asset: {assetPath}");
                return;
            }

            var sceneService = _serviceRegistry.GetService<ISceneService>();
            if (sceneService == null)
            {
                Logger.Error("ISceneService not found in ServiceRegistry");
                return;
            }

            if (asset is Entity entityAsset)
            {
                Stack<Entity> entities = new();
                entities.Push(entityAsset);
                while (entities.Count > 0)
                {
                    var entity = entities.Pop();
                    sceneService.AddToScene(entity, entity.Parent);
                    foreach (var child in entity.Children)
                    {
                        entities.Push(child);
                    }
                }
                Logger.Info($"Entity loaded: {assetPath}");
            }
        }

        private void Update(float delta)
        {
            var sceneService = _serviceRegistry.GetService<ISceneService>();
            if (sceneService == null)
            {
                Logger.Warn("ISceneService not found during update");
                return;
            }

            foreach (var entity in sceneService.GetEntities())
            {
                entity.Update(delta);
                sceneService.UpdateTransform(entity, entity.Transform);
            }
            // Logger.Debug($"Updating scene with delta: {delta}");
        }
    }
}