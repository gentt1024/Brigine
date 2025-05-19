using System;
using System.Collections.Generic;

namespace Brigine.Core
{
    public class Framework
    {
        private readonly ServiceRegistry _registry;
        private readonly AssetManager _assetManager;

        public ServiceRegistry Services => _registry;
        public ILogger Logger => Services.GetService<ILogger>();

        public void Start()
        {
            var updateService = _registry.GetService<IUpdateService>();
            if (updateService == null)
            {
                Logger.Error("IUpdateService not found in ServiceRegistry");
                throw new InvalidOperationException("IUpdateService is required");
            }
            updateService.RegisterUpdate(Update);
        }

        public Framework()
        {
            _registry = new ServiceRegistry();
            _assetManager = new AssetManager(_registry);
            Logger.Info("Framework initialized");
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

            var sceneService = _registry.GetService<ISceneService>();
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
            var sceneService = _registry.GetService<ISceneService>();
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