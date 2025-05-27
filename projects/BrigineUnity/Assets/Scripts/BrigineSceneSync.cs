using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Brigine.Communication.Client;
using Brigine.Communication.Protos;

/// <summary>
/// Brigine场景同步组件 - 展示事件驱动的实时场景同步
/// 这个组件演示了新的"数据即服务"架构模式
/// </summary>
public class BrigineSceneSync : MonoBehaviour
{
    [Header("Connection Settings")]
    [SerializeField] private string serverAddress = "http://localhost:50051";
    [SerializeField] private bool autoConnect = true;
    [SerializeField] private bool enableEventLogging = true;

    [Header("Sync Settings")]
    [SerializeField] private bool syncTransforms = true;
    [SerializeField] private bool syncEntityCreation = true;
    [SerializeField] private bool syncEntityDeletion = true;

    [Header("Test Controls")]
    [SerializeField] private KeyCode addEntityKey = KeyCode.Space;
    [SerializeField] private KeyCode removeEntityKey = KeyCode.Delete;

    private BrigineClient _client;
    private string _frameworkId;
    private CancellationTokenSource _eventsCancellation;
    private readonly Dictionary<string, GameObject> _entityToGameObject = new();
    private readonly Dictionary<GameObject, string> _gameObjectToEntity = new();

    async void Start()
    {
        if (autoConnect)
        {
            await ConnectToBrigineServer();
        }
    }

    public async Task ConnectToBrigineServer()
    {
        try
        {
            Debug.Log("[BrigineSync] Connecting to Brigine server...");
            
            _client = new BrigineClient(serverAddress);
            
            // 启动远程框架
            var startResponse = await _client.StartFrameworkAsync(new[] { "Unity" });
            if (!startResponse.Success)
            {
                Debug.LogError($"[BrigineSync] Failed to start framework: {startResponse.ErrorMessage}");
                return;
            }
            
            _frameworkId = startResponse.FrameworkId;
            Debug.Log($"[BrigineSync] Connected! Framework ID: {_frameworkId}");

            // 开始监听场景事件
            await StartListeningToSceneEvents();
            
            Debug.Log("[BrigineSync] Scene event listening started. Ready for real-time sync!");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BrigineSync] Connection failed: {ex.Message}");
        }
    }

    private async Task StartListeningToSceneEvents()
    {
        _eventsCancellation = new CancellationTokenSource();
        
        // 在后台任务中监听事件
        _ = Task.Run(async () =>
        {
            try
            {
                await _client.StartSceneEventsAsync(_frameworkId, OnSceneEventReceived, _eventsCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[BrigineSync] Scene events listening cancelled");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BrigineSync] Scene events error: {ex.Message}");
            }
        });
    }

    private void OnSceneEventReceived(SceneEvent sceneEvent)
    {
        if (enableEventLogging)
        {
            Debug.Log($"[BrigineSync] Received event: {sceneEvent.EventType} for entity {sceneEvent.EntityId}");
        }

        // 切换到主线程执行Unity操作
        UnityMainThreadDispatcher.Enqueue(() =>
        {
            switch (sceneEvent.EventType)
            {
                case SceneEventType.EntityAdded:
                    if (syncEntityCreation)
                        HandleEntityAdded(sceneEvent);
                    break;
                    
                case SceneEventType.EntityRemoved:
                    if (syncEntityDeletion)
                        HandleEntityRemoved(sceneEvent);
                    break;
                    
                case SceneEventType.EntityTransformUpdated:
                    if (syncTransforms)
                        HandleEntityTransformUpdated(sceneEvent);
                    break;
            }
        });
    }

    private async void HandleEntityAdded(SceneEvent sceneEvent)
    {
        // 检查是否已经存在
        if (_entityToGameObject.ContainsKey(sceneEvent.EntityId))
        {
            Debug.LogWarning($"[BrigineSync] Entity {sceneEvent.EntityId} already exists locally");
            return;
        }

        // 获取完整的实体信息
        try
        {
            var entityInfoResponse = await _client.GetEntityInfoAsync(_frameworkId, sceneEvent.EntityId);
            if (entityInfoResponse.Success)
            {
                CreateLocalGameObject(entityInfoResponse.Entity);
                Debug.Log($"[BrigineSync] Created local GameObject for remote entity: {sceneEvent.EntityId}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BrigineSync] Failed to get entity info: {ex.Message}");
        }
    }

    private void HandleEntityRemoved(SceneEvent sceneEvent)
    {
        if (_entityToGameObject.TryGetValue(sceneEvent.EntityId, out var gameObject))
        {
            _entityToGameObject.Remove(sceneEvent.EntityId);
            _gameObjectToEntity.Remove(gameObject);
            
            Destroy(gameObject);
            Debug.Log($"[BrigineSync] Removed local GameObject for entity: {sceneEvent.EntityId}");
        }
    }

    private async void HandleEntityTransformUpdated(SceneEvent sceneEvent)
    {
        if (_entityToGameObject.TryGetValue(sceneEvent.EntityId, out var gameObject))
        {
            try
            {
                var entityInfoResponse = await _client.GetEntityInfoAsync(_frameworkId, sceneEvent.EntityId);
                if (entityInfoResponse.Success)
                {
                    ApplyTransformToGameObject(gameObject, entityInfoResponse.Entity.Transform);
                    Debug.Log($"[BrigineSync] Updated transform for entity: {sceneEvent.EntityId}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BrigineSync] Failed to update transform: {ex.Message}");
            }
        }
    }

    private void CreateLocalGameObject(EntityInfo entityInfo)
    {
        var go = new GameObject(entityInfo.Name);
        
        // 应用变换
        ApplyTransformToGameObject(go, entityInfo.Transform);
        
        // 添加视觉标识
        var renderer = go.AddComponent<MeshRenderer>();
        var meshFilter = go.AddComponent<MeshFilter>();
        meshFilter.mesh = CreateCubeMesh();
        renderer.material = CreateDefaultMaterial();
        
        // 存储映射关系
        _entityToGameObject[entityInfo.EntityId] = go;
        _gameObjectToEntity[go] = entityInfo.EntityId;
        
        // 添加标签以便识别
        go.tag = "BrigineEntity";
    }

    private void ApplyTransformToGameObject(GameObject go, Brigine.Communication.Protos.Transform transform)
    {
        go.transform.position = new Vector3(transform.Position.X, transform.Position.Y, transform.Position.Z);
        go.transform.rotation = new Quaternion(transform.Rotation.X, transform.Rotation.Y, transform.Rotation.Z, transform.Rotation.W);
        go.transform.localScale = new Vector3(transform.Scale.X, transform.Scale.Y, transform.Scale.Z);
    }

    private Mesh CreateCubeMesh()
    {
        var mesh = new Mesh();
        mesh.vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f)
        };
        mesh.triangles = new int[]
        {
            0, 2, 1, 0, 3, 2, 2, 3, 4, 2, 4, 5, 1, 2, 5, 1, 5, 6,
            0, 7, 4, 0, 4, 3, 5, 4, 7, 5, 7, 6, 0, 6, 7, 0, 1, 6
        };
        mesh.RecalculateNormals();
        return mesh;
    }

    private Material CreateDefaultMaterial()
    {
        var material = new Material(Shader.Find("Standard"));
        material.color = UnityEngine.Random.ColorHSV();
        return material;
    }

    void Update()
    {
        // 测试控制
        if (Input.GetKeyDown(addEntityKey))
        {
            _ = AddTestEntity();
        }
        
        if (Input.GetKeyDown(removeEntityKey))
        {
            _ = RemoveRandomEntity();
        }
    }

    private async Task AddTestEntity()
    {
        if (_client == null || string.IsNullOrEmpty(_frameworkId))
        {
            Debug.LogWarning("[BrigineSync] Not connected to server");
            return;
        }

        try
        {
            var entity = BrigineClient.CreateEntity(
                $"TestEntity_{DateTime.Now:HHmmss}",
                "Mesh",
                BrigineClient.CreateTransform(
                    UnityEngine.Random.Range(-5f, 5f),
                    UnityEngine.Random.Range(0f, 3f),
                    UnityEngine.Random.Range(-5f, 5f)
                )
            );

            var response = await _client.AddEntityToSceneAsync(_frameworkId, entity);
            if (response.Success)
            {
                Debug.Log($"[BrigineSync] Added test entity: {response.EntityId}");
            }
            else
            {
                Debug.LogError($"[BrigineSync] Failed to add entity: {response.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BrigineSync] Error adding entity: {ex.Message}");
        }
    }

    private async Task RemoveRandomEntity()
    {
        if (_entityToGameObject.Count == 0)
        {
            Debug.Log("[BrigineSync] No entities to remove");
            return;
        }

        try
        {
            var entityIds = new List<string>(_entityToGameObject.Keys);
            var randomEntityId = entityIds[UnityEngine.Random.Range(0, entityIds.Count)];

            var response = await _client.RemoveEntityFromSceneAsync(_frameworkId, randomEntityId);
            if (response.Success)
            {
                Debug.Log($"[BrigineSync] Removed entity: {randomEntityId}");
            }
            else
            {
                Debug.LogError($"[BrigineSync] Failed to remove entity: {response.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BrigineSync] Error removing entity: {ex.Message}");
        }
    }

    public async void DisconnectFromServer()
    {
        if (_client != null && !string.IsNullOrEmpty(_frameworkId))
        {
            try
            {
                await _client.StopFrameworkAsync(_frameworkId);
                Debug.Log("[BrigineSync] Framework stopped");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BrigineSync] Failed to stop framework: {ex.Message}");
            }
        }

        _eventsCancellation?.Cancel();
        _client?.Dispose();
        _client = null;
        _frameworkId = null;
        
        Debug.Log("[BrigineSync] Disconnected from server");
    }

    void OnDestroy()
    {
        DisconnectFromServer();
    }

    // Unity Inspector按钮
    [ContextMenu("Connect to Server")]
    public void ConnectToServerMenu()
    {
        _ = ConnectToBrigineServer();
    }

    [ContextMenu("Disconnect from Server")]
    public void DisconnectFromServerMenu()
    {
        DisconnectFromServer();
    }

    [ContextMenu("Add Test Entity")]
    public void AddTestEntityMenu()
    {
        _ = AddTestEntity();
    }

    [ContextMenu("Remove Random Entity")]
    public void RemoveRandomEntityMenu()
    {
        _ = RemoveRandomEntity();
    }
}

/// <summary>
/// Unity主线程调度器 - 用于从后台线程调度Unity API调用
/// </summary>
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance;
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();

    public static void Enqueue(Action action)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }
} 