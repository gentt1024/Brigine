using System;
using Godot;
using Brigine.Core;
using System.Collections.Generic;
using Quaternion = Godot.Quaternion;
using Vector3 = Godot.Vector3;
using Godot.Collections;
using Array = Godot.Collections.Array;

namespace Brigine.Godot
{
    public class GodotFunctionProvider : IFunctionProvider
    {
        private readonly System.Collections.Generic.Dictionary<Type, object> _serviceCache = new();

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

        private class GodotSceneService : ISceneService
        {
            private readonly System.Collections.Generic.Dictionary<Entity, Node3D> _primToNode = new();

            public IEnumerable<Entity> GetEntities() => _primToNode.Keys;

            public void AddToScene(Entity entity, Entity parent)
            {
                var node = new Node3D();
                
                // 检查是否有MeshComponent
                var meshComp = entity.GetComponent<Core.Components.MeshComponent>();
                if (meshComp != null && meshComp.MeshData != null)
                {
                    // 创建ArrayMesh
                    var arrayMesh = new ArrayMesh();
                    var arrays = new Array();
                    arrays.Resize((int)Mesh.ArrayType.Max);
                    
                    Vector3[] vertices = null;
                    Vector3[] normals = null;
                    Vector2[] uvs = null;
                    int[] indices = null;
                    
                    // 设置顶点
                    if (meshComp.MeshData.Vertices is { Length: > 0 })
                    {
                        vertices = new Vector3[meshComp.MeshData.Vertices.Length / 3];
                        for (int i = 0; i < vertices.Length; i++)
                        {
                            vertices[i] = new Vector3(
                                meshComp.MeshData.Vertices[i * 3],
                                meshComp.MeshData.Vertices[i * 3 + 1],
                                meshComp.MeshData.Vertices[i * 3 + 2]
                            );
                        }
                        arrays[(int)Mesh.ArrayType.Vertex] = vertices;
                    }
                    
                    // 设置法线
                    if (meshComp.MeshData.Normals is { Length: > 0 })
                    {
                        normals = new Vector3[meshComp.MeshData.Normals.Length / 3];
                        for (int i = 0; i < normals.Length; i++)
                        {
                            normals[i] = new Vector3(
                                meshComp.MeshData.Normals[i * 3],
                                meshComp.MeshData.Normals[i * 3 + 1],
                                meshComp.MeshData.Normals[i * 3 + 2]
                            );
                        }
                        arrays[(int)Mesh.ArrayType.Normal] = normals;
                    }
                    
                    // 设置UV
                    if (meshComp.MeshData.UVs is { Length: > 0 })
                    {
                        uvs = new Vector2[meshComp.MeshData.UVs.Length / 2];
                        for (int i = 0; i < uvs.Length; i++)
                        {
                            uvs[i] = new Vector2(
                                meshComp.MeshData.UVs[i * 2],
                                meshComp.MeshData.UVs[i * 2 + 1]
                            );
                        }
                        arrays[(int)Mesh.ArrayType.TexUV] = uvs;
                    }
                    
                    // 设置索引
                    if (meshComp.MeshData.FaceVertexCounts is {Length: > 0} faceVertexCounts
                        && meshComp.MeshData.FaceVertexIndices is { Length: > 0 } faceVertexIndices)
                    {
                        // 创建三角形索引数组
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
                        
                        // 将三角形索引转换为Godot数组
                        indices = triangulatedIndices.ToArray();
                        GD.Print($"[INFO] Created {indices.Length / 3} triangles from {faceVertexCounts.Length} faces");
                        
                        arrays[(int)Mesh.ArrayType.Index] = indices;
                    }
                    else
                    {
                        GD.PushWarning("[WARN] Mesh has no valid indices");
                    }
                    
                    // 创建材质（可选）
                    var material = new StandardMaterial3D
                    {
                        AlbedoColor = new Color(1, 0, 0), // 红色
                        NormalEnabled = true,
                    };
                    
                    // 创建表面
                    
                    if (vertices != null && indices != null)
                    {
                        // 创建SurfaceTool
                        SurfaceTool st = new SurfaceTool();
                        st.Begin(Mesh.PrimitiveType.Triangles);

                        // 添加顶点和索引
                        foreach (var idx in indices)
                        {
                            st.AddVertex(vertices[idx]);
                        }

                        // if (normals == null)
                        {
                            // 自动生成法线
                            st.GenerateNormals();
                        }
                        
                        // gen tangent
                        st.GenerateTangents();

                        // 创建Mesh
                        st.Commit(arrayMesh);
                    }
                    
                    arrayMesh.SurfaceSetMaterial(0, material);
                    
                    // 设置网格
                    var meshInstance = new MeshInstance3D
                    {
                        Mesh = arrayMesh
                    };
                    node.AddChild(meshInstance);
                    
                    GD.Print($"[INFO] Created custom mesh with {meshComp.MeshData.Vertices.Length / 3} vertices, {meshComp.MeshData.FaceVertexIndices.Length / 3} triangles");
                }
                
                _primToNode[entity] = node;

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