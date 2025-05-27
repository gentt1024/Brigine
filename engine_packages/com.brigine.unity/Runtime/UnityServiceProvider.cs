using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using UnityEngine;
using Brigine.Core;
using ILogger = Brigine.Core.ILogger;
using Transform = Brigine.Core.Transform;

namespace Brigine.Unity
{
    /// <summary>
    /// Unity引擎的服务提供者，注册Unity特定的服务实现
    /// </summary>
    public static class UnityServiceProvider
    {
        /// <summary>
        /// 注册Unity特定的服务到服务注册表
        /// </summary>
        public static void RegisterUnityServices(IServiceRegistry serviceRegistry)
        {
            serviceRegistry.RegisterSingleton<ISceneService>(() => new UnitySceneService(serviceRegistry));
            serviceRegistry.RegisterSingleton<IUpdateService>(() => new UnityUpdateService(serviceRegistry));
            serviceRegistry.RegisterSingleton<ILogger, UnityLogger>();
            
            UnityEngine.Debug.Log("[Brigine] Unity services registered successfully");
        }
    }

    /// <summary>
    /// Unity场景服务实现
    /// </summary>
    public class UnitySceneService : ISceneService
    {
        private readonly ConcurrentDictionary<string, Entity> _entities = new();
        private readonly ConcurrentDictionary<string, GameObject> _entityToGameObject = new();
        private readonly ILogger _logger;

        public UnitySceneService() : this(null) { }

        public UnitySceneService(IServiceRegistry serviceRegistry)
        {
            _logger = serviceRegistry?.GetService<ILogger>() ?? new UnityLogger();
        }

        public IEnumerable<Entity> GetEntities() => _entities.Values;

        public void AddToScene(Entity entity, Entity parent)
        {
            if (entity == null) return;

            // 创建GameObject
            GameObject go = new GameObject(entity.Name ?? "BrigineEntity");
            
            // 检查是否有MeshComponent
            var meshComp = entity.GetComponent<Core.Components.MeshComponent>();
            if (meshComp != null && meshComp.MeshData != null)
            {
                CreateMeshFromComponent(go, meshComp);
            }

            // 设置变换
            ApplyTransformToGameObject(go, entity.Transform);

            // 处理父子关系
            if (parent != null && _entityToGameObject.TryGetValue(parent.Id, out var parentGO))
            {
                go.transform.SetParent(parentGO.transform);
            }

            // 存储映射关系
            _entities[entity.Id] = entity;
            _entityToGameObject[entity.Id] = go;

            _logger?.Info($"Entity '{entity.Name}' added to Unity scene");
        }

        public void UpdateTransform(Entity entity, Transform transform)
        {
            if (entity == null || !_entityToGameObject.TryGetValue(entity.Id, out var go)) 
                return;

            ApplyTransformToGameObject(go, transform);
            entity.Transform = transform;
            
            _logger?.Debug($"Transform updated for entity '{entity.Name}'");
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
                if (_entityToGameObject.TryRemove(entityId, out var go))
                {
                    UnityEngine.Object.Destroy(go);
                }
                _logger?.Info($"Entity '{entity.Name}' removed from Unity scene");
            }
        }

        private void CreateMeshFromComponent(GameObject go, Core.Components.MeshComponent meshComp)
        {
            var meshFilter = go.AddComponent<MeshFilter>();
            var meshRenderer = go.AddComponent<MeshRenderer>();
            
            var mesh = new Mesh();
            var meshData = meshComp.MeshData;

            // 设置顶点
            if (meshData.Vertices?.Length > 0)
            {
                var vertices = new UnityEngine.Vector3[meshData.Vertices.Length / 3];
                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i] = new UnityEngine.Vector3(
                        meshData.Vertices[i * 3],
                        meshData.Vertices[i * 3 + 1],
                        meshData.Vertices[i * 3 + 2]
                    );
                }
                mesh.vertices = vertices;
            }

            // 设置索引
            if (meshData.FaceVertexCounts?.Length > 0 && meshData.FaceVertexIndices?.Length > 0)
            {
                var triangles = TriangulateFromFaces(meshData.FaceVertexCounts, meshData.FaceVertexIndices);
                mesh.triangles = triangles;
            }
            else if (meshData.FaceVertexIndices?.Length > 0)
            {
                mesh.triangles = meshData.FaceVertexIndices;
            }

            // 设置法线
            if (meshData.Normals?.Length > 0)
            {
                var normals = new UnityEngine.Vector3[meshData.Normals.Length / 3];
                for (int i = 0; i < normals.Length; i++)
                {
                    normals[i] = new UnityEngine.Vector3(
                        meshData.Normals[i * 3],
                        meshData.Normals[i * 3 + 1],
                        meshData.Normals[i * 3 + 2]
                    );
                }
                mesh.normals = normals;
            }
            else
            {
                mesh.RecalculateNormals();
            }

            // 设置UV
            if (meshData.UVs?.Length > 0)
            {
                var uvs = new UnityEngine.Vector2[meshData.UVs.Length / 2];
                for (int i = 0; i < uvs.Length; i++)
                {
                    uvs[i] = new UnityEngine.Vector2(
                        meshData.UVs[i * 2],
                        meshData.UVs[i * 2 + 1]
                    );
                }
                mesh.uv = uvs;
            }

            mesh.RecalculateTangents();
            meshFilter.mesh = mesh;
            meshRenderer.material = GetDefaultMaterial();

            _logger?.Info($"Created Unity mesh with {mesh.vertexCount} vertices");
        }

        private int[] TriangulateFromFaces(int[] faceVertexCounts, int[] faceVertexIndices)
        {
            var triangles = new List<int>();
            int indexOffset = 0;

            for (int faceIndex = 0; faceIndex < faceVertexCounts.Length; faceIndex++)
            {
                int vertexCount = faceVertexCounts[faceIndex];

                if (vertexCount == 3)
                {
                    // 三角形
                    triangles.Add(faceVertexIndices[indexOffset]);
                    triangles.Add(faceVertexIndices[indexOffset + 1]);
                    triangles.Add(faceVertexIndices[indexOffset + 2]);
                }
                else if (vertexCount == 4)
                {
                    // 四边形 -> 两个三角形
                    triangles.Add(faceVertexIndices[indexOffset]);
                    triangles.Add(faceVertexIndices[indexOffset + 1]);
                    triangles.Add(faceVertexIndices[indexOffset + 2]);

                    triangles.Add(faceVertexIndices[indexOffset]);
                    triangles.Add(faceVertexIndices[indexOffset + 2]);
                    triangles.Add(faceVertexIndices[indexOffset + 3]);
                }
                else if (vertexCount > 4)
                {
                    // 扇形三角剖分
                    for (int i = 1; i < vertexCount - 1; i++)
                    {
                        triangles.Add(faceVertexIndices[indexOffset]);
                        triangles.Add(faceVertexIndices[indexOffset + i]);
                        triangles.Add(faceVertexIndices[indexOffset + i + 1]);
                    }
                }

                indexOffset += vertexCount;
            }

            return triangles.ToArray();
        }

        private void ApplyTransformToGameObject(GameObject go, Transform transform)
        {
            if (go == null) return;

            go.transform.position = new UnityEngine.Vector3(
                transform.Position.X,
                transform.Position.Y,
                transform.Position.Z
            );

            go.transform.rotation = new UnityEngine.Quaternion(
                transform.Rotation.X,
                transform.Rotation.Y,
                transform.Rotation.Z,
                transform.Rotation.W
            );

            // Note: Scale is commented out in Transform class, uncomment when available
            // go.transform.localScale = new UnityEngine.Vector3(
            //     transform.Scale.X,
            //     transform.Scale.Y,
            //     transform.Scale.Z
            // );
        }

        private Material GetDefaultMaterial()
        {
            // 尝试使用URP/Built-in管线的默认材质
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? 
                        Shader.Find("Standard") ?? 
                        Shader.Find("Diffuse");
            
            return new Material(shader);
        }
    }

    /// <summary>
    /// Unity更新服务实现
    /// </summary>
    public class UnityUpdateService : IUpdateService
    {
        private readonly List<Action<float>> _updateCallbacks = new();
        private BrigineUpdater _updater;
        private readonly ILogger _logger;

        public UnityUpdateService() : this(null) { }

        public UnityUpdateService(IServiceRegistry serviceRegistry)
        {
            _logger = serviceRegistry?.GetService<ILogger>() ?? new UnityLogger();
        }

        public void RegisterUpdate(Action<float> updateCallback)
        {
            _updateCallbacks.Add(updateCallback);

            if (_updater == null)
            {
                var go = new GameObject("BrigineUpdater");
                UnityEngine.Object.DontDestroyOnLoad(go);
                _updater = go.AddComponent<BrigineUpdater>();
                _updater.SetUpdateCallback(OnUpdate);
                _logger?.Info("Unity update service started");
            }
        }

        public void Stop()
        {
            if (_updater != null)
            {
                UnityEngine.Object.Destroy(_updater.gameObject);
                _updater = null;
            }
            _updateCallbacks.Clear();
            _logger?.Info("Unity update service stopped");
        }

        private void OnUpdate(float deltaTime)
        {
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
        }
    }

    /// <summary>
    /// Unity更新器MonoBehaviour
    /// </summary>
    public class BrigineUpdater : MonoBehaviour
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

    /// <summary>
    /// Unity日志服务实现
    /// </summary>
    public class UnityLogger : ILogger
    {
        public void Info(string message) => UnityEngine.Debug.Log($"[INFO] {message}");
        public void Warn(string message) => UnityEngine.Debug.LogWarning($"[WARN] {message}");
        public void Error(string message) => UnityEngine.Debug.LogError($"[ERROR] {message}");
        public void Debug(string message) => UnityEngine.Debug.Log($"[DEBUG] {message}");
    }
} 