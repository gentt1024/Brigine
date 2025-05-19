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
            if (type == typeof(ISceneService))
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

        private class UnitySceneService : ISceneService
        {
            private readonly Dictionary<Entity, GameObject> _entityToGameObject = new();

            public IEnumerable<Entity> GetEntities() => _entityToGameObject.Keys;

            public void AddToScene(Entity entity, Entity parent)
            {
                GameObject go = new GameObject(entity.GetType().Name);
                
                // 检查是否有MeshComponent
                var meshComp = entity.GetComponent<Core.Components.MeshComponent>();
                if (meshComp != null && meshComp.MeshData != null)
                {
                    // 创建带有自定义网格的游戏对象
                    var meshFilter = go.AddComponent<MeshFilter>();
                    var meshRenderer = go.AddComponent<MeshRenderer>();
                    
                    // 从MeshData创建Unity的Mesh
                    var mesh = new Mesh();
                    
                    // 设置顶点数据
                    if (meshComp.MeshData.Vertices is { Length: > 0 })
                    {
                        var vertices = new UnityEngine.Vector3[meshComp.MeshData.Vertices.Length / 3];
                        for (int i = 0; i < vertices.Length; i++)
                        {
                            vertices[i] = new UnityEngine.Vector3(
                                meshComp.MeshData.Vertices[i * 3],
                                meshComp.MeshData.Vertices[i * 3 + 1],
                                meshComp.MeshData.Vertices[i * 3 + 2]
                            );
                        }
                        mesh.vertices = vertices;
                        
                        // 设置索引数据 - 处理FaceVertexCounts和FaceVertexIndices
                        if (meshComp.MeshData.FaceVertexCounts is { Length: > 0 } faceVertexCounts
                            && meshComp.MeshData.FaceVertexIndices is { Length: > 0 } faceVertexIndices)
                        {
                            // 创建三角形索引列表
                            var triangulatedIndices = new List<int>();
                            
                            // 面顶点计数索引的起始位置
                            int indexOffset = 0;
                            
                            // 遍历每个面
                            for (int faceIndex = 0; faceIndex < faceVertexCounts.Length; faceIndex++)
                            {
                                // 获取当前面的顶点数
                                int vertexCount = faceVertexCounts[faceIndex];
                                
                                // 如果是三角形，直接添加索引
                                if (vertexCount == 3)
                                {
                                    triangulatedIndices.Add(faceVertexIndices[indexOffset]);
                                    triangulatedIndices.Add(faceVertexIndices[indexOffset + 1]);
                                    triangulatedIndices.Add(faceVertexIndices[indexOffset + 2]);
                                }
                                // 如果是四边形，拆分为两个三角形
                                else if (vertexCount == 4)
                                {
                                    // 第一个三角形 (0,1,2)
                                    triangulatedIndices.Add(faceVertexIndices[indexOffset]);
                                    triangulatedIndices.Add(faceVertexIndices[indexOffset + 1]);
                                    triangulatedIndices.Add(faceVertexIndices[indexOffset + 2]);
                                    
                                    // 第二个三角形 (0,2,3)
                                    triangulatedIndices.Add(faceVertexIndices[indexOffset]);
                                    triangulatedIndices.Add(faceVertexIndices[indexOffset + 2]);
                                    triangulatedIndices.Add(faceVertexIndices[indexOffset + 3]);
                                }
                                // 如果是n边形（n>4），使用扇形三角剖分
                                else if (vertexCount > 4)
                                {
                                    // 扇形三角剖分 (以顶点0为共享点)
                                    for (int i = 1; i < vertexCount - 1; i++)
                                    {
                                        triangulatedIndices.Add(faceVertexIndices[indexOffset]);
                                        triangulatedIndices.Add(faceVertexIndices[indexOffset + i]);
                                        triangulatedIndices.Add(faceVertexIndices[indexOffset + i + 1]);
                                    }
                                }
                                
                                // 更新索引偏移量
                                indexOffset += vertexCount;
                            }
                            
                            // 设置三角形索引到网格
                            mesh.triangles = triangulatedIndices.ToArray();
                            UnityEngine.Debug.Log($"[INFO] Created {triangulatedIndices.Count / 3} triangles from {faceVertexCounts.Length} faces");
                        }
                        else if (meshComp.MeshData.FaceVertexIndices is { Length: > 0 })
                        {
                            // 直接使用三角形索引
                            mesh.triangles = meshComp.MeshData.FaceVertexIndices;
                        }
                        else
                        {
                            UnityEngine.Debug.LogWarning("[WARN] Mesh has no valid indices");
                        }
                    }
                    
                    // 设置法线数据
                    if (meshComp.MeshData.Normals is { Length: > 0 })
                    {
                        var normals = new UnityEngine.Vector3[meshComp.MeshData.Normals.Length / 3];
                        for (int i = 0; i < normals.Length; i++)
                        {
                            normals[i] = new UnityEngine.Vector3(
                                meshComp.MeshData.Normals[i * 3],
                                meshComp.MeshData.Normals[i * 3 + 1],
                                meshComp.MeshData.Normals[i * 3 + 2]
                            );
                        }
                        mesh.normals = normals;
                    }
                    else
                    {
                        mesh.RecalculateNormals();
                    }
                    
                    // 设置UV数据
                    if (meshComp.MeshData.UVs is { Length: > 0 })
                    {
                        var uvs = new UnityEngine.Vector2[meshComp.MeshData.UVs.Length / 2];
                        for (int i = 0; i < uvs.Length; i++)
                        {
                            uvs[i] = new UnityEngine.Vector2(
                                meshComp.MeshData.UVs[i * 2],
                                meshComp.MeshData.UVs[i * 2 + 1]
                            );
                        }
                        mesh.uv = uvs;
                    }
                    
                    // 重新计算切线
                    mesh.RecalculateTangents();
                    
                    // 设置Mesh
                    meshFilter.mesh = mesh;
                    
                    // 创建材质
                    var material = new Material(Shader.Find("Standard"))
                    {
                        color = Color.red // 设置为红色，与Godot示例一致
                    };
                    meshRenderer.material = material;
                    
                    UnityEngine.Debug.Log($"[INFO] Created custom mesh with {mesh.vertexCount} vertices, {mesh.triangles.Length / 3} triangles");
                }
                
                _entityToGameObject[entity] = go;
                
                if (parent != null && _entityToGameObject.TryGetValue(parent, out var parentObject))
                {
                    go.transform.SetParent(parentObject.transform);
                }
                else
                {
                    // 如果没有父对象，则添加到场景根节点
                    // 在Unity中，不需要显式添加到场景，对象创建后自动添加
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