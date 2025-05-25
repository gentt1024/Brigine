using System;
using System.Collections.Generic;

namespace Brigine.Core
{
    public interface IAssetSerializer
    {
        object Load(string assetPath);
    }

    public interface ISceneService
    {
        IEnumerable<Entity> GetEntities();
        void AddToScene(Entity entity, Entity parent);
        void UpdateTransform(Entity entity, Transform transform);
        Entity GetEntity(string entityId);
        void RemoveFromScene(string entityId);
    }

    public interface IUpdateService
    {
        void RegisterUpdate(Action<float> updateCallback);
        void Stop();
    }
    
    public interface ILogger
    {
        void Info(string message);
        void Warn(string message);
        void Error(string message);
        void Debug(string message);
    }
}