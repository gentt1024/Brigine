using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using Brigine.Core;
using ILogger = Brigine.Core.ILogger;
using Transform = Brigine.Core.Transform;

namespace Brigine.Godot
{
    /// <summary>
    /// Godot引擎的服务提供者，注册Godot特定的服务实现
    /// </summary>
    public static class GodotServiceProvider
    {
        /// <summary>
        /// 注册Godot特定的服务到服务注册表
        /// </summary>
        public static void RegisterGodotServices(IServiceRegistry serviceRegistry)
        {
            serviceRegistry.RegisterSingleton<ISceneService>(() => new GodotSceneService(serviceRegistry));
            serviceRegistry.RegisterSingleton<IUpdateService>(() => new GodotUpdateService(serviceRegistry));
            serviceRegistry.RegisterSingleton<ILogger, GodotLogger>();
            
            GD.Print("[Brigine] Godot services registered successfully");
        }
    }

    /// <summary>
    /// Godot场景服务实现
    /// </summary>
    public class GodotSceneService : ISceneService
    {
        private readonly ConcurrentDictionary<string, Entity> _entities = new();
        private readonly ConcurrentDictionary<string, Node3D> _entityToNode = new();
        private readonly ILogger _logger;
        private Node3D _sceneRoot;

        public GodotSceneService() : this(null) { }

        public GodotSceneService(IServiceRegistry serviceRegistry)
        {
            _logger = serviceRegistry?.GetService<ILogger>() ?? new GodotLogger();
            
            // 获取或创建场景根节点
            _sceneRoot = GetOrCreateSceneRoot();
        }

        public IEnumerable<Entity> GetEntities() => _entities.Values;

        public void AddToScene(Entity entity, Entity parent)
        {
            if (entity == null) return;

            // 创建Node3D
            var node = new Node3D();
            node.Name = entity.Name ?? "BrigineEntity";
            
            // 检查是否有MeshComponent
            var meshComp = entity.GetComponent<Core.Components.MeshComponent>();
            if (meshComp != null && meshComp.MeshData != null)
            {
                CreateMeshFromComponent(node, meshComp);
            }

            // 设置变换
            ApplyTransformToNode(node, entity.Transform);

            // 处理父子关系
            Node3D parentNode = _sceneRoot;
            if (parent != null && _entityToNode.TryGetValue(parent.Id, out var parentGodotNode))
            {
                parentNode = parentGodotNode;
            }

            // 添加到场景
            parentNode.AddChild(node);

            // 存储映射关系
            _entities[entity.Id] = entity;
            _entityToNode[entity.Id] = node;

            _logger?.Info($"Entity '{entity.Name}' added to Godot scene");
        }

        public void UpdateTransform(Entity entity, Transform transform)
        {
            if (entity == null || !_entityToNode.TryGetValue(entity.Id, out var node)) 
                return;

            ApplyTransformToNode(node, transform);
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
                if (_entityToNode.TryRemove(entityId, out var node))
                {
                    node.QueueFree();
                }
                _logger?.Info($"Entity '{entity.Name}' removed from Godot scene");
            }
        }

        private Node3D GetOrCreateSceneRoot()
        {
            // 尝试获取当前场景的根节点
            var sceneTree = Engine.GetMainLoop() as SceneTree;
            if (sceneTree?.CurrentScene != null)
            {
                // 查找或创建Brigine根节点
                var brigineRoot = sceneTree.CurrentScene.FindChild("BrigineRoot") as Node3D;
                if (brigineRoot == null)
                {
                    brigineRoot = new Node3D();
                    brigineRoot.Name = "BrigineRoot";
                    sceneTree.CurrentScene.AddChild(brigineRoot);
                }
                return brigineRoot;
            }

            // 如果没有当前场景，创建一个新的根节点
            var root = new Node3D();
            root.Name = "BrigineRoot";
            return root;
        }

        private void CreateMeshFromComponent(Node3D node, Core.Components.MeshComponent meshComp)
        {
            var meshInstance = new MeshInstance3D();
            var arrayMesh = new ArrayMesh();
            var meshData = meshComp.MeshData;

            var arrays = new System.Collections.Generic.List<object>();
            for (int i = 0; i < (int)Mesh.ArrayType.Max; i++)
            {
                arrays.Add(null);
            }

            // 设置顶点
            if (meshData.Vertices?.Length > 0)
            {
                var vertices = new Vector3[meshData.Vertices.Length / 3];
                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i] = new Vector3(
                        meshData.Vertices[i * 3],
                        meshData.Vertices[i * 3 + 1],
                        meshData.Vertices[i * 3 + 2]
                    );
                }
                arrays[(int)Mesh.ArrayType.Vertex] = vertices;
            }

            // 设置索引
            if (meshData.FaceVertexCounts?.Length > 0 && meshData.FaceVertexIndices?.Length > 0)
            {
                var indices = TriangulateFromFaces(meshData.FaceVertexCounts, meshData.FaceVertexIndices);
                arrays[(int)Mesh.ArrayType.Index] = indices;
            }
            else if (meshData.FaceVertexIndices?.Length > 0)
            {
                arrays[(int)Mesh.ArrayType.Index] = meshData.FaceVertexIndices;
            }

            // 设置法线
            if (meshData.Normals?.Length > 0)
            {
                var normals = new Vector3[meshData.Normals.Length / 3];
                for (int i = 0; i < normals.Length; i++)
                {
                    normals[i] = new Vector3(
                        meshData.Normals[i * 3],
                        meshData.Normals[i * 3 + 1],
                        meshData.Normals[i * 3 + 2]
                    );
                }
                arrays[(int)Mesh.ArrayType.Normal] = normals;
            }

            // 设置UV
            if (meshData.UVs?.Length > 0)
            {
                var uvs = new Vector2[meshData.UVs.Length / 2];
                for (int i = 0; i < uvs.Length; i++)
                {
                    uvs[i] = new Vector2(
                        meshData.UVs[i * 2],
                        meshData.UVs[i * 2 + 1]
                    );
                }
                arrays[(int)Mesh.ArrayType.TexUV] = uvs;
            }

            // 创建网格 - 简化实现，跳过复杂的数组设置
            // 在实际使用中，需要根据具体的Godot版本调整
            try
            {
                // arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
                // meshInstance.Mesh = arrayMesh;
                
                // 暂时创建一个简单的立方体作为占位符
                var boxMesh = new BoxMesh();
                boxMesh.Size = Vector3.One;
                meshInstance.Mesh = boxMesh;
            }
            catch (Exception ex)
            {
                _logger?.Warn($"Failed to create mesh from component: {ex.Message}");
                // 创建默认立方体
                var boxMesh = new BoxMesh();
                meshInstance.Mesh = boxMesh;
            }

            // 设置默认材质
            var material = new StandardMaterial3D();
            material.AlbedoColor = Colors.White;
            meshInstance.MaterialOverride = material;

            node.AddChild(meshInstance);
            _logger?.Info($"Created Godot mesh placeholder");
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

        private void ApplyTransformToNode(Node3D node, Transform transform)
        {
            if (node == null) return;

            node.Position = new Vector3(
                transform.Position.X,
                transform.Position.Y,
                transform.Position.Z
            );

            node.Quaternion = new Quaternion(
                transform.Rotation.X,
                transform.Rotation.Y,
                transform.Rotation.Z,
                transform.Rotation.W
            );

            // Note: Scale is commented out in Transform class, uncomment when available
            // node.Scale = new Vector3(
            //     transform.Scale.X,
            //     transform.Scale.Y,
            //     transform.Scale.Z
            // );
        }
    }

    /// <summary>
    /// Godot更新服务实现
    /// </summary>
    public class GodotUpdateService : IUpdateService
    {
        private readonly List<Action<float>> _updateCallbacks = new();
        private BrigineUpdater _updater;
        private readonly ILogger _logger;

        public GodotUpdateService() : this(null) { }

        public GodotUpdateService(IServiceRegistry serviceRegistry)
        {
            _logger = serviceRegistry?.GetService<ILogger>() ?? new GodotLogger();
        }

        public void RegisterUpdate(Action<float> updateCallback)
        {
            _updateCallbacks.Add(updateCallback);

            if (_updater == null)
            {
                _updater = new BrigineUpdater();
                _updater.SetUpdateCallback(OnUpdate);
                
                // 添加到场景树
                var sceneTree = Engine.GetMainLoop() as SceneTree;
                if (sceneTree?.CurrentScene != null)
                {
                    sceneTree.CurrentScene.AddChild(_updater);
                }
                
                _logger?.Info("Godot update service started");
            }
        }

        public void Stop()
        {
            if (_updater != null)
            {
                _updater.QueueFree();
                _updater = null;
            }
            _updateCallbacks.Clear();
            _logger?.Info("Godot update service stopped");
        }

        private void OnUpdate(double deltaTime)
        {
            foreach (var callback in _updateCallbacks)
            {
                try
                {
                    callback((float)deltaTime);
                }
                catch (Exception ex)
                {
                    _logger?.Error($"Update callback error: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Godot更新器Node
    /// </summary>
    public partial class BrigineUpdater : Node
    {
        private Action<double> _updateCallback;

        public void SetUpdateCallback(Action<double> callback)
        {
            _updateCallback = callback;
        }

        public override void _Process(double delta)
        {
            _updateCallback?.Invoke(delta);
        }
    }

    /// <summary>
    /// Godot日志服务实现
    /// </summary>
    public class GodotLogger : ILogger
    {
        public void Info(string message) => GD.Print($"[INFO] {message}");
        public void Warn(string message) => GD.PrintErr($"[WARN] {message}");
        public void Error(string message) => GD.PrintErr($"[ERROR] {message}");
        public void Debug(string message) => GD.Print($"[DEBUG] {message}");
    }
} 