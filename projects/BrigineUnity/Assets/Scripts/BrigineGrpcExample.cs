using System;
using System.Collections.Generic;
using Brigine.Communication.Client;
using Brigine.Communication.Unity;
using UnityEngine;
using Brigine.Core;
using Brigine.Unity;

// 注意：需要安装YetAnotherHttpHandler包才能使用gRPC功能
using Cysharp.Net.Http;

/// <summary>
/// Brigine gRPC协作示例 - 展示本地Framework + 远程协作的模式
/// 本地Framework处理引擎特定操作，gRPC客户端处理协作同步
/// </summary>
public class BrigineGrpcExample : MonoBehaviour
{
    [Header("gRPC Settings")]
    [SerializeField] private string serverAddress = "http://localhost:50051";
    [SerializeField] private bool connectOnStart = false;
    [SerializeField] private bool useLocalFramework = true;

    [Header("Test Settings")]
    [SerializeField] private string testAssetPath = "models/cube.usda";

    // 本地Framework实例 - 处理Unity特定操作
    private Framework _localFramework;
    
    // gRPC客户端 - 处理协作同步
    private BrigineClient _grpcClient;
    private string _sessionId;

    void Start()
    {
        if (useLocalFramework)
        {
            InitializeLocalFramework();
        }

        if (connectOnStart)
        {
            // ConnectToServer();
            Debug.LogWarning("[Brigine] gRPC功能需要安装YetAnotherHttpHandler包。请参考README.md中的说明。");
        }
    }

    /// <summary>
    /// 初始化本地Framework实例
    /// </summary>
    private void InitializeLocalFramework()
    {
        try
        {
            var serviceRegistry = new ServiceRegistry();
            UnityServiceProvider.RegisterUnityServices(serviceRegistry);
            
            _localFramework = new Framework(serviceRegistry, "Unity");
            _localFramework.Start();
            
            Debug.Log("[Brigine] Local framework initialized");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Brigine] Failed to initialize local framework: {ex.Message}");
        }
    }

    /// <summary>
    /// 连接到协作服务器
    /// </summary>
    public async void ConnectToServer()
    {
        try
        {
            // 创建YetAnotherHttpHandler
            var httpHandler = new YetAnotherHttpHandler();
            
            // 创建gRPC客户端
            _grpcClient = new BrigineClient(serverAddress, httpHandler);
            
            Debug.Log($"[Brigine] Connecting to server: {serverAddress}");

            // 创建协作会话
            var sessionResponse = await _grpcClient.CreateSessionAsync(
                "UnityTestProject", 
                "UnityUser",
                new Dictionary<string, string> { { "client", "Unity" } }
            );

            if (sessionResponse.Success)
            {
                _sessionId = sessionResponse.SessionId;
                Debug.Log($"[Brigine] Collaboration session created: {_sessionId}");

                // 开始监听场景事件
                await StartSceneEventListening();
            }
            else
            {
                Debug.LogError($"[Brigine] Failed to create session: {sessionResponse.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Brigine] gRPC connection failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 开始监听场景事件
    /// </summary>
    private async System.Threading.Tasks.Task StartSceneEventListening()
    {
        try
        {
            await _grpcClient.StartSceneEventsAsync(_sessionId, "UnityUser", OnSceneEventReceived);
            Debug.Log("[Brigine] Scene event listening started");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Brigine] Failed to start scene event listening: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理接收到的场景事件
    /// </summary>
    private void OnSceneEventReceived(Brigine.Communication.Protos.SceneChangeEvent sceneEvent)
    {
        Debug.Log($"[Brigine] Scene event received: {sceneEvent.ChangeType} on entity {sceneEvent.EntityId}");
        
        // 在主线程中更新本地场景
        UnityMainThreadDispatcher.Enqueue(() =>
        {
            UpdateLocalSceneFromEvent(sceneEvent);
        });
    }

    /// <summary>
    /// 根据远程事件更新本地场景
    /// </summary>
    private void UpdateLocalSceneFromEvent(Brigine.Communication.Protos.SceneChangeEvent sceneEvent)
    {
        if (_localFramework == null) return;

        try
        {
            switch (sceneEvent.ChangeType)
            {
                case Brigine.Communication.Protos.SceneChangeType.EntityAdded:
                    Debug.Log($"[Brigine] Adding entity to local scene: {sceneEvent.EntityId}");
                    // 这里可以添加从远程数据创建本地实体的逻辑
                    break;
                    
                case Brigine.Communication.Protos.SceneChangeType.EntityRemoved:
                    Debug.Log($"[Brigine] Removing entity from local scene: {sceneEvent.EntityId}");
                    _localFramework.RemoveEntity(sceneEvent.EntityId);
                    break;
                    
                case Brigine.Communication.Protos.SceneChangeType.EntityModified:
                    Debug.Log($"[Brigine] Updating entity in local scene: {sceneEvent.EntityId}");
                    // 这里可以添加更新本地实体的逻辑
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Brigine] Failed to update local scene: {ex.Message}");
        }
    }

    /// <summary>
    /// 加载资产到本地场景
    /// </summary>
    public void LoadAssetLocally()
    {
        if (_localFramework == null)
        {
            Debug.LogWarning("[Brigine] Local framework not initialized");
            return;
        }

        try
        {
            _localFramework.LoadAsset(testAssetPath);
            Debug.Log($"[Brigine] Asset loaded locally: {testAssetPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Brigine] Asset loading failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 添加当前GameObject到协作场景
    /// </summary>
    public async void AddEntityToCollaborativeScene()
    {
        if (_grpcClient == null || string.IsNullOrEmpty(_sessionId))
        {
            Debug.LogWarning("[Brigine] Not connected to collaboration server");
            return;
        }

        try
        {
            var entity = BrigineUnityExtensions.CreateEntityFromGameObject(this.gameObject);

            var createResponse = await _grpcClient.CreateEntityAsync(_sessionId, "UnityUser", entity);

            if (createResponse.Success)
            {
                Debug.Log($"[Brigine] Entity added to collaborative scene: {createResponse.EntityId}");
            }
            else
            {
                Debug.LogError($"[Brigine] Failed to add entity: {createResponse.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Brigine] Entity addition failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取本地Framework状态
    /// </summary>
    private string GetLocalFrameworkStatus()
    {
        if (_localFramework == null)
            return "Local framework not initialized";
            
        return $"Local Framework Status:\n" +
               $"- Engine: {_localFramework.EngineType}\n" +
               $"- Running: {_localFramework.IsRunning}\n" +
               $"- Entities: {_localFramework.GetSceneEntities().Count()}";
    }

    /// <summary>
    /// 断开服务器连接
    /// </summary>
    public async void DisconnectFromServer()
    {
        if (_grpcClient != null && !string.IsNullOrEmpty(_sessionId))
        {
            try
            {
                await _grpcClient.LeaveSessionAsync(_sessionId, "UnityUser");
                Debug.Log("[Brigine] Left collaboration session");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Brigine] Failed to leave session: {ex.Message}");
            }
        }

        _grpcClient?.Dispose();
        _grpcClient = null;
        _sessionId = null;
        
        Debug.Log("[Brigine] Disconnected from server");
    }

    void OnDestroy()
    {
        // DisconnectFromServer();
        
        // 清理本地Framework
        _localFramework?.Dispose();
    }

    // Unity Inspector按钮
    [ContextMenu("Connect to Server")]
    public void ConnectToServerMenu()
    {
        Debug.LogWarning("[Brigine] gRPC功能需要安装YetAnotherHttpHandler包。请参考README.md中的说明。");
        // ConnectToServer();
    }

    [ContextMenu("Load Asset Locally")]
    public void LoadAssetLocallyMenu()
    {
        LoadAssetLocally();
    }

    [ContextMenu("Add Entity to Collaborative Scene")]
    public void AddEntityToCollaborativeSceneMenu()
    {
        Debug.LogWarning("[Brigine] gRPC功能需要安装YetAnotherHttpHandler包。请参考README.md中的说明。");
        // AddEntityToCollaborativeScene();
    }

    [ContextMenu("Get Local Framework Status")]
    public void GetLocalFrameworkStatusMenu()
    {
        Debug.Log(GetLocalFrameworkStatus());
    }

    [ContextMenu("Disconnect from Server")]
    public void DisconnectFromServerMenu()
    {
        Debug.LogWarning("[Brigine] gRPC功能需要安装YetAnotherHttpHandler包。请参考README.md中的说明。");
        // DisconnectFromServer();
    }
} 
 