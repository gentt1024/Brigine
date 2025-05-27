using System;
using System.IO;
using UnityEngine;
using Brigine.Core;
using Brigine.Unity;

/// <summary>
/// Brigine框架启动组件 - 简化的本地Framework管理
/// 每个Unity客户端创建一个Framework实例来处理本地引擎集成
/// </summary>
public class FrameworkStartup : MonoBehaviour
{
    [Header("Framework Settings")]
    [SerializeField] private bool autoStart = true;
    [SerializeField] private bool loadTestAsset = true;
    [SerializeField] private string testAssetPath = @"models\cube.usda";

    private Framework _framework;

    void Start()
    {
        if (autoStart)
        {
            InitializeFramework();
        }
    }

    /// <summary>
    /// 初始化本地Framework实例
    /// </summary>
    public void InitializeFramework()
    {
        try
        {
            // 创建服务注册表
            var serviceRegistry = new ServiceRegistry();
            
            // 注册Unity特定的服务
            UnityServiceProvider.RegisterUnityServices(serviceRegistry);
            
            // 创建并启动框架 - 指定引擎类型
            _framework = new Framework(serviceRegistry, "Unity");
            _framework.Start();
            
            Debug.Log("[Brigine] Framework initialized and started successfully");
            
            // 加载测试资源（如果启用）
            if (loadTestAsset)
            {
                LoadTestAsset();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Brigine] Failed to initialize framework: {ex.Message}");
        }
    }

    private void LoadTestAsset()
    {
        try
        {
            // 构建资源路径
            string assetsPath = Application.dataPath;
            string projectRoot = Directory.GetParent(assetsPath).FullName;
            string parentDirectory = Directory.GetParent(projectRoot).FullName;
            string externalAssetsPath = Path.Combine(parentDirectory, "assets");
            string modelPath = Path.Combine(externalAssetsPath, testAssetPath);
            
            if (File.Exists(modelPath))
            {
                _framework.LoadAsset(modelPath);
                Debug.Log($"[Brigine] Test asset loaded: {modelPath}");
            }
            else
            {
                Debug.LogWarning($"[Brigine] Test asset not found: {modelPath}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Brigine] Failed to load test asset: {ex.Message}");
        }
    }

    /// <summary>
    /// 停止Framework
    /// </summary>
    public void StopFramework()
    {
        if (_framework != null && _framework.IsRunning)
        {
            _framework.Stop();
            Debug.Log("[Brigine] Framework stopped");
        }
    }

    /// <summary>
    /// 获取当前Framework实例
    /// </summary>
    public Framework GetFramework() => _framework;

    /// <summary>
    /// 获取Framework状态信息
    /// </summary>
    public string GetFrameworkStatus()
    {
        if (_framework == null)
            return "Framework not initialized";
            
        return $"Framework Status:\n" +
               $"- Engine: {_framework.EngineType}\n" +
               $"- Running: {_framework.IsRunning}\n" +
               $"- Entities: {_framework.GetSceneEntities().Count()}";
    }

    void OnDestroy()
    {
        // 使用Dispose方法进行完整清理
        _framework?.Dispose();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            StopFramework();
        }
        else if (_framework != null && !_framework.IsRunning)
        {
            _framework.Start();
        }
    }

    // Unity Inspector按钮
    [ContextMenu("Initialize Framework")]
    public void InitializeFrameworkMenu()
    {
        InitializeFramework();
    }

    [ContextMenu("Stop Framework")]
    public void StopFrameworkMenu()
    {
        StopFramework();
    }

    [ContextMenu("Get Framework Status")]
    public void GetFrameworkStatusMenu()
    {
        Debug.Log(GetFrameworkStatus());
    }

    [ContextMenu("Load Test Asset")]
    public void LoadTestAssetMenu()
    {
        LoadTestAsset();
    }
}
