using System;

namespace Brigine.Core
{
    public class Framework
    {
        private readonly ServiceRegistry _registry;
        private readonly AssetManager _assetManager;
        private readonly ILogger _logger;

        public Framework(ServiceRegistry registry)
        {
            this._registry = registry ?? throw new ArgumentNullException(nameof(registry));
            this._assetManager = new AssetManager(registry);
            this._logger = registry.GetService<ILogger>();

            var updateService = registry.GetService<IUpdateService>();
            if (updateService == null)
            {
                _logger.Error("IUpdateService not found in ServiceRegistry");
                throw new InvalidOperationException("IUpdateService is required");
            }
            updateService.RegisterUpdate(Update);

            _logger.Info("Framework initialized");
        }

        public void LoadScene(string assetPath)
        {
            _logger.Info($"Loading scene from {assetPath}");
            var entity = _assetManager.LoadAsset(assetPath);
            if (entity == null)
            {
                _logger.Error($"Failed to load asset: {assetPath}");
                return;
            }

            var sceneService = _registry.GetService<ISceneService>();
            if (sceneService == null)
            {
                _logger.Error("ISceneService not found in ServiceRegistry");
                return;
            }

            sceneService.AddToScene(entity, null);
            _logger.Info($"Scene loaded: {assetPath}");
        }

        private void Update(float delta)
        {
            var sceneService = _registry.GetService<ISceneService>();
            if (sceneService == null)
            {
                _logger.Warn("ISceneService not found during update");
                return;
            }

            foreach (var entity in sceneService.GetEntities())
            {
                entity.Update(delta);
            }
            _logger.Debug($"Updating scene with delta: {delta}");
        }
    }
}