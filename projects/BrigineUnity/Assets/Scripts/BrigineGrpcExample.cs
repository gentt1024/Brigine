using System;
using System.Collections.Generic;
using Brigine.Communication.Client;
using Brigine.Communication.Unity;
using UnityEngine;
using Brigine.Core;
using Brigine.Unity;

// 注意：需要安装YetAnotherHttpHandler包才能使用gRPC功能
using Cysharp.Net.Http;

public class BrigineGrpcExample : MonoBehaviour
{
    [Header("gRPC Settings")]
    [SerializeField] private string serverAddress = "http://localhost:50051";
    [SerializeField] private bool connectOnStart = false;
    [SerializeField] private bool useLocalFramework = true;

    [Header("Test Settings")]
    [SerializeField] private string testAssetPath = "models/cube.usda";

    // 注意：以下代码需要安装YetAnotherHttpHandler包
    // private BrigineUnityClient _grpcClient;
    private Framework _localFramework;
    private string _remoteFrameworkId;
    private BrigineClient _grpcClient;

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

    private void InitializeLocalFramework()
    {
        try
        {
            var serviceRegistry = new ServiceRegistry();
            UnityServiceProvider.RegisterUnityServices(serviceRegistry);
            
            _localFramework = new Framework(serviceRegistry);
            _localFramework.Start();
            
            Debug.Log("[Brigine] Local framework initialized");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Brigine] Failed to initialize local framework: {ex.Message}");
        }
    }

    public async void ConnectToServer()
    {
        try
        {
            // 创建YetAnotherHttpHandler
            var httpHandler = new YetAnotherHttpHandler();
            
            // 创建Unity专用的gRPC客户端
            _grpcClient = new BrigineClient(serverAddress, httpHandler);
            
            Debug.Log($"[Brigine] Connecting to server: {serverAddress}");

            // 启动远程框架
            var startResponse = await _grpcClient.StartFrameworkAsync(
                new[] { "Unity" },
                new Dictionary<string, string> { { "client", "Unity" } }
            );

            if (startResponse.Success)
            {
                _remoteFrameworkId = startResponse.FrameworkId;
                Debug.Log($"[Brigine] Remote framework started: {_remoteFrameworkId}");

                // 获取框架状态
                await GetFrameworkStatus();
            }
            else
            {
                Debug.LogError($"[Brigine] Failed to start remote framework: {startResponse.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Brigine] gRPC connection failed: {ex.Message}");
        }
    }

    public async void LoadAssetRemotely()
    {
        if (_grpcClient == null || string.IsNullOrEmpty(_remoteFrameworkId))
        {
            Debug.LogWarning("[Brigine] Not connected to remote server");
            return;
        }

        try
        {
            var loadResponse = await _grpcClient.LoadAssetAsync(
                _remoteFrameworkId,
                testAssetPath
            );

            if (loadResponse.Success)
            {
                Debug.Log($"[Brigine] Asset loaded remotely: {loadResponse.AssetId}");
            }
            else
            {
                Debug.LogError($"[Brigine] Failed to load asset: {loadResponse.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Brigine] Asset loading failed: {ex.Message}");
        }
    }

    public async void AddEntityToRemoteScene()
    {
        if (_grpcClient == null || string.IsNullOrEmpty(_remoteFrameworkId))
        {
            Debug.LogWarning("[Brigine] Not connected to remote server");
            return;
        }

        try
        {
            var entity = BrigineUnityExtensions.CreateEntityFromGameObject(this.gameObject);

            var addResponse = await _grpcClient.AddEntityToSceneAsync(
                _remoteFrameworkId,
                entity
            );

            if (addResponse.Success)
            {
                Debug.Log($"[Brigine] Entity added to remote scene: {addResponse.EntityId}");
            }
            else
            {
                Debug.LogError($"[Brigine] Failed to add entity: {addResponse.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Brigine] Entity addition failed: {ex.Message}");
        }
    }

    private async System.Threading.Tasks.Task GetFrameworkStatus()
    {
        try
        {
            var statusResponse = await _grpcClient.GetFrameworkStatusAsync(_remoteFrameworkId);
            if (statusResponse.Success)
            {
                Debug.Log($"[Brigine] Framework Status:");
                Debug.Log($"  - Running: {statusResponse.Status.IsRunning}");
                // Debug.Log($"  - Services: {string.Join(", ", statusResponse.Status.AvailableServices)}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Brigine] Failed to get status: {ex.Message}");
        }
    }

    public async void DisconnectFromServer()
    {
        if (_grpcClient != null && !string.IsNullOrEmpty(_remoteFrameworkId))
        {
            try
            {
                await _grpcClient.StopFrameworkAsync(_remoteFrameworkId);
                Debug.Log("[Brigine] Remote framework stopped");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Brigine] Failed to stop remote framework: {ex.Message}");
            }
        }

        _grpcClient?.Dispose();
        _grpcClient = null;
        _remoteFrameworkId = null;
        Debug.Log("[Brigine] Disconnected from server");
    }

    void OnDestroy()
    {
        // DisconnectFromServer();
        
        if (_localFramework != null && _localFramework.IsRunning)
        {
            _localFramework.Stop();
        }
    }

    // Unity Inspector按钮
    [ContextMenu("Connect to Server")]
    public void ConnectToServerMenu()
    {
        Debug.LogWarning("[Brigine] gRPC功能需要安装YetAnotherHttpHandler包。请参考README.md中的说明。");
        // ConnectToServer();
    }

    [ContextMenu("Load Asset Remotely")]
    public void LoadAssetRemotelyMenu()
    {
        Debug.LogWarning("[Brigine] gRPC功能需要安装YetAnotherHttpHandler包。请参考README.md中的说明。");
        // LoadAssetRemotely();
    }

    [ContextMenu("Add Entity to Remote Scene")]
    public void AddEntityToRemoteSceneMenu()
    {
        Debug.LogWarning("[Brigine] gRPC功能需要安装YetAnotherHttpHandler包。请参考README.md中的说明。");
        // AddEntityToRemoteScene();
    }

    [ContextMenu("Disconnect from Server")]
    public void DisconnectFromServerMenu()
    {
        Debug.LogWarning("[Brigine] gRPC功能需要安装YetAnotherHttpHandler包。请参考README.md中的说明。");
        // DisconnectFromServer();
    }
} 
 