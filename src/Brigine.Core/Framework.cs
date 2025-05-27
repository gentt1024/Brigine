using System;
using System.Collections.Generic;

namespace Brigine.Core
{
    /// <summary>
    /// Brigine核心框架 - 跨引擎3D场景协作的本地实例
    /// 每个客户端（Unity/Godot/Unreal）创建一个Framework实例来管理本地引擎集成
    /// </summary>
    public class Framework : IDisposable
    {
        private readonly IServiceRegistry _serviceRegistry;
        private readonly AssetManager _assetManager;
        private bool _isRunning = false;
        private bool _disposed = false;

        public IServiceRegistry Services => _serviceRegistry;
        public ILogger Logger => Services.GetService<ILogger>();
        public bool IsRunning => _isRunning;
        public string EngineType { get; private set; }

        /// <summary>
        /// 创建Framework实例
        /// </summary>
        /// <param name="serviceRegistry">服务注册表，如果为null则创建默认实例</param>
        /// <param name="engineType">引擎类型标识（用于日志和调试）</param>
        public Framework(IServiceRegistry serviceRegistry = null, string engineType = "Unknown")
        {
            _serviceRegistry = serviceRegistry ?? new ServiceRegistry();
            EngineType = engineType;
            
            // 确保注册了默认服务
            _serviceRegistry.RegisterDefaultServices();
            
            _assetManager = new AssetManager(_serviceRegistry);
            Logger.Info($"Framework initialized for {EngineType} engine");
        }

        /// <summary>
        /// 启动框架，开始更新循环
        /// </summary>
        public void Start()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Framework));
                
            if (_isRunning)
            {
                Logger.Warn("Framework is already running");
                return;
            }

            var updateService = _serviceRegistry.GetService<IUpdateService>();
            if (updateService == null)
            {
                Logger.Error("IUpdateService not found in ServiceRegistry");
                throw new InvalidOperationException("IUpdateService is required to start Framework");
            }
            
            updateService.RegisterUpdate(Update);
            _isRunning = true;
            Logger.Info($"Framework started for {EngineType} engine");
        }

        /// <summary>
        /// 停止框架
        /// </summary>
        public void Stop()
        {
            if (_disposed)
                return;
                
            if (!_isRunning)
            {
                Logger.Warn("Framework is not running");
                return;
            }

            var updateService = _serviceRegistry.GetService<IUpdateService>();
            updateService?.Stop();
            
            _isRunning = false;
            Logger.Info($"Framework stopped for {EngineType} engine");
        }

        /// <summary>
        /// 加载资产到场景
        /// 支持USD、FBX、OBJ等多种格式
        /// </summary>
        /// <param name="assetPath">资产文件路径</param>
        public void LoadAsset(string assetPath)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Framework));
                
            Logger.Info($"Loading asset from {assetPath}");
            
            try
            {
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
                    AddEntityToScene(entityAsset, sceneService);
                    Logger.Info($"Asset loaded successfully: {assetPath}");
                }
                else
                {
                    Logger.Warn($"Asset is not an Entity: {assetPath}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception loading asset {assetPath}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 递归添加实体及其子实体到场景
        /// </summary>
        private void AddEntityToScene(Entity entity, ISceneService sceneService)
        {
            Stack<Entity> entities = new();
            entities.Push(entity);
            
            while (entities.Count > 0)
            {
                var currentEntity = entities.Pop();
                sceneService.AddToScene(currentEntity, currentEntity.Parent);
                
                // 添加子实体到处理栈
                foreach (var child in currentEntity.Children)
                {
                    entities.Push(child);
                }
            }
        }

        /// <summary>
        /// 框架更新循环 - 由IUpdateService调用
        /// </summary>
        private void Update(float deltaTime)
        {
            if (_disposed || !_isRunning)
                return;
                
            try
            {
                var sceneService = _serviceRegistry.GetService<ISceneService>();
                if (sceneService == null)
                {
                    Logger.Warn("ISceneService not found during update");
                    return;
                }

                // 更新所有场景实体
                foreach (var entity in sceneService.GetEntities())
                {
                    entity.Update(deltaTime);
                    sceneService.UpdateTransform(entity, entity.Transform);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception during framework update: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取当前场景中的所有实体
        /// </summary>
        public IEnumerable<Entity> GetSceneEntities()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Framework));
                
            var sceneService = _serviceRegistry.GetService<ISceneService>();
            return sceneService?.GetEntities() ?? new List<Entity>();
        }

        /// <summary>
        /// 添加实体到场景
        /// </summary>
        public void AddEntity(Entity entity, Entity parent = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Framework));
                
            var sceneService = _serviceRegistry.GetService<ISceneService>();
            if (sceneService != null)
            {
                sceneService.AddToScene(entity, parent);
                Logger.Info($"Entity added to scene: {entity.Name}");
            }
            else
            {
                Logger.Error("ISceneService not found");
            }
        }

        /// <summary>
        /// 从场景移除实体
        /// </summary>
        public void RemoveEntity(string entityId)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Framework));
                
            var sceneService = _serviceRegistry.GetService<ISceneService>();
            if (sceneService != null)
            {
                sceneService.RemoveFromScene(entityId);
                Logger.Info($"Entity removed from scene: {entityId}");
            }
            else
            {
                Logger.Error("ISceneService not found");
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
                
            Stop();
            
            // 清理资源
            if (_assetManager is IDisposable disposableAssetManager)
            {
                disposableAssetManager.Dispose();
            }
            
            _disposed = true;
            Logger?.Info($"Framework disposed for {EngineType} engine");
        }
    }
}