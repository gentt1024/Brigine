using System;
using System.Collections.Generic;

namespace Brigine.Core
{
    /// <summary>
    /// 资产序列化接口 - 负责加载各种格式的3D资产
    /// 支持USD、FBX、OBJ、GLTF等格式
    /// </summary>
    public interface IAssetSerializer
    {
        /// <summary>
        /// 加载指定路径的资产
        /// </summary>
        /// <param name="assetPath">资产文件路径</param>
        /// <returns>加载的资产对象，通常为Entity</returns>
        object Load(string assetPath);
    }

    /// <summary>
    /// 场景服务接口 - 管理引擎特定的场景操作
    /// 每个引擎（Unity/Godot/Unreal）都有自己的实现
    /// </summary>
    public interface ISceneService
    {
        /// <summary>
        /// 获取场景中的所有实体
        /// </summary>
        /// <returns>实体集合</returns>
        IEnumerable<Entity> GetEntities();
        
        /// <summary>
        /// 添加实体到场景
        /// </summary>
        /// <param name="entity">要添加的实体</param>
        /// <param name="parent">父实体，可为null</param>
        void AddToScene(Entity entity, Entity parent);
        
        /// <summary>
        /// 更新实体的变换信息
        /// </summary>
        /// <param name="entity">要更新的实体</param>
        /// <param name="transform">新的变换信息</param>
        void UpdateTransform(Entity entity, Transform transform);
        
        /// <summary>
        /// 根据ID获取实体
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <returns>找到的实体，如果不存在则返回null</returns>
        Entity GetEntity(string entityId);
        
        /// <summary>
        /// 从场景中移除实体
        /// </summary>
        /// <param name="entityId">要移除的实体ID</param>
        void RemoveFromScene(string entityId);
    }

    /// <summary>
    /// 场景事件通知接口 - 用于实时同步场景变更
    /// 支持跨引擎的实时协作功能
    /// </summary>
    public interface ISceneEventNotifier
    {
        /// <summary>
        /// 实体添加到场景时触发
        /// </summary>
        event Action<Entity> EntityAdded;
        
        /// <summary>
        /// 实体从场景移除时触发
        /// </summary>
        event Action<string> EntityRemoved;
        
        /// <summary>
        /// 实体变换更新时触发
        /// </summary>
        event Action<Entity> EntityTransformUpdated;
        
        /// <summary>
        /// 实体属性变更时触发
        /// </summary>
        event Action<Entity, string> EntityPropertyChanged;
    }

    /// <summary>
    /// 更新服务接口 - 管理框架的更新循环
    /// 提供类似游戏引擎的Update机制
    /// </summary>
    public interface IUpdateService
    {
        /// <summary>
        /// 注册更新回调函数
        /// </summary>
        /// <param name="updateCallback">每帧调用的更新函数，参数为deltaTime</param>
        void RegisterUpdate(Action<float> updateCallback);
        
        /// <summary>
        /// 停止更新服务
        /// </summary>
        void Stop();
    }
    
    /// <summary>
    /// 日志接口 - 提供统一的日志记录功能
    /// 各引擎可以实现自己的日志输出方式
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// 记录信息日志
        /// </summary>
        /// <param name="message">日志消息</param>
        void Info(string message);
        
        /// <summary>
        /// 记录警告日志
        /// </summary>
        /// <param name="message">日志消息</param>
        void Warn(string message);
        
        /// <summary>
        /// 记录错误日志
        /// </summary>
        /// <param name="message">日志消息</param>
        void Error(string message);
        
        /// <summary>
        /// 记录调试日志
        /// </summary>
        /// <param name="message">日志消息</param>
        void Debug(string message);
    }
}