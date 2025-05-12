using System;
using Godot;
using Brigine.Core;
using System.Collections.Generic;
using Quaternion = Godot.Quaternion;
using Vector3 = Godot.Vector3;

namespace Brigine.Godot
{
    public class GodotFunctionProvider : IFunctionProvider
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
                service = new GodotAssetLoader() as T;
            }
            else if (type == typeof(ISceneService))
            {
                service = new GodotSceneService() as T;
            }
            else if (type == typeof(IUpdateService))
            {
                service = new GodotUpdateService() as T;
            }
            else if (type == typeof(ILogger))
            {
                service = new GodotLogger() as T;
            }

            if (service != null)
            {
                _serviceCache[type] = service;
            }

            return service;
        }

        private class GodotAssetLoader : IAssetLoader
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
                // using var file = FileAccess.Open(assetPath, FileAccess.ModeFlags.Read);
                // return file?.GetAsText();
            }
        }

        private class GodotSceneService : ISceneService
        {
            private readonly Dictionary<Entity, Node3D> _primToNode = new();

            public IEnumerable<Entity> GetEntities() => _primToNode.Keys;

            public void AddToScene(Entity prim, Entity parent)
            {
                var node = new Node3D();
                var mesh = new MeshInstance3D { Mesh = new BoxMesh() };
                node.AddChild(mesh);
                _primToNode[prim] = node;

                if (parent != null && _primToNode.TryGetValue(parent, out var parentObject))
                {
                    parentObject.AddChild(node);
                }
                else
                {
                    (Engine.GetMainLoop() as SceneTree)?.CurrentScene.AddChild(node);
                }
            }

            public void UpdateTransform(Entity prim, Transform transform)
            {
                if (_primToNode.TryGetValue(prim, out var node))
                {
                    node.Position = new Vector3(transform.Position.X, transform.Position.Y, transform.Position.Z);
                    node.Quaternion = new Quaternion(transform.Rotation.X, transform.Rotation.Y, transform.Rotation.Z, transform.Rotation.W);
                }
            }
        }

        private class GodotUpdateService : IUpdateService
        {
            private Action<float> _updateCallback;

            public GodotUpdateService()
            {
                if (Engine.GetMainLoop() is SceneTree sceneTree)
                {
                    sceneTree.Connect("process_frame", 
                        Callable.From(() => _updateCallback?.Invoke((float)sceneTree.CurrentScene.GetProcessDeltaTime())));
                }
                else
                {
                    GD.Print("Engine.GetMainLoop() is not SceneTree");
                }
            }

            public void RegisterUpdate(Action<float> updateCallback)
            {
                this._updateCallback = updateCallback;
            }
        }
        
        private class GodotLogger : ILogger
        {
            public void Info(string message) => GD.Print($"[INFO] {message}");
            public void Warn(string message) => GD.PushWarning($"[WARN] {message}");
            public void Error(string message) => GD.PushError($"[ERROR] {message}");
            public void Debug(string message) => GD.Print($"[DEBUG] {message}");
        }
    }
}