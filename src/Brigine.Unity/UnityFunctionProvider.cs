using System;
using UnityEngine;
using Brigine.Core;
using System.Collections.Generic;
using ILogger = Brigine.Core.ILogger;
using Transform = Brigine.Core.Transform;

namespace Brigine.Unity
{
    public class UnityFunctionProvider : IFunctionProvider
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
            if (type == typeof(IAssetLoader))
            {
                service = new UnityAssetLoader() as T;
            }
            else if (type == typeof(ISceneService))
            {
                service = new UnitySceneService() as T;
            }
            else if (type == typeof(IUpdateService))
            {
                service = new UnityUpdateService() as T;
            }
            else if (type == typeof(ILogger))
            {
                service = new UnityLogger() as T;
            }

            if (service != null)
            {
                _serviceCache[type] = service;
            }

            return service;
        }

        private class UnityAssetLoader : IAssetLoader
        {
            public string LoadAsset(string assetPath)
            {
                try
                {
                    return System.IO.File.ReadAllText(assetPath);
                }
                catch
                {
                    return null;
                }
            }
        }

        private class UnitySceneService : ISceneService
        {
            private readonly Dictionary<Entity, GameObject> _entityToGameObject = new();

            public IEnumerable<Entity> GetEntities() => _entityToGameObject.Keys;

            public void AddToScene(Entity prim, Entity parent)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _entityToGameObject[prim] = go;
                if (parent != null && _entityToGameObject.TryGetValue(parent, out var parentObject))
                {
                    go.transform.SetParent(parentObject.transform);
                }
            }

            public void UpdateTransform(Entity prim, Transform transform)
            {
                if (_entityToGameObject.TryGetValue(prim, out var go))
                {
                    go.transform.position = new Vector3(transform.Position.X, transform.Position.Y, transform.Position.Z);
                    go.transform.rotation = new Quaternion(transform.Rotation.X, transform.Rotation.Y, transform.Rotation.Z, transform.Rotation.W);
                }
            }
        }

        private class UnityUpdateService : IUpdateService
        {
            private Action<float> _updateCallback;

            public UnityUpdateService()
            {
                var go = new GameObject("BrigineUpdater");
                var updater = go.AddComponent<BrigineUpdater>();
                updater.SetUpdateCallback(delta => _updateCallback?.Invoke(delta));
            }

            public void RegisterUpdate(Action<float> updateCallback)
            {
                this._updateCallback = updateCallback;
            }
        }

        private class BrigineUpdater : MonoBehaviour
        {
            private Action<float> _updateCallback;

            public void SetUpdateCallback(Action<float> callback)
            {
                _updateCallback = callback;
            }

            private void Update()
            {
                _updateCallback?.Invoke(Time.deltaTime);
            }
        }
        
        private class UnityLogger : ILogger
        {
            public void Info(string message) => UnityEngine.Debug.Log($"[INFO] {message}");
            public void Warn(string message) => UnityEngine.Debug.LogWarning($"[WARN] {message}");
            public void Error(string message) => UnityEngine.Debug.LogError($"[ERROR] {message}");
            public void Debug(string message) => UnityEngine.Debug.Log($"[DEBUG] {message}");
        }
    }
}